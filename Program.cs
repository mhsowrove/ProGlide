using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace AirPodsSwitch
{
    public sealed class Preferences
    {
        public string AirPodsId = "";
        public string SpeakersId = "";
        public string Match = "AirPods";
        public bool AutoSwitch;
        public bool AnimatePopup = true;
        public string ThemeName = "Dark";
        public bool TrackBattery = true;
    }
    public static class Program
    {
        [STAThread] public static int Main(string[] args)
        {
            try { return Run(args); }
            catch (Exception error)
            {
                int index = Array.IndexOf(args, "--report");
                if (index >= 0 && index + 1 < args.Length) File.WriteAllText(args[index + 1], "FAIL: " + error);
                else MessageBox.Show(error.Message, "ProGlide");
                return 1;
            }
        }
        static int Run(string[] args)
        {
            if (args.Contains("--diagnose")) return Diagnostics.Run(args);
            if (args.Contains("--render-test")) return Diagnostics.Render(args);
            if (args.Contains("--render-preferences")) return Diagnostics.RenderPreferences(args);
            if (args.Contains("--render-about")) return Diagnostics.RenderAbout(args);
            if (args.Contains("--test-animation")) return Diagnostics.TestAnimation(args);
            if (args.Contains("--battery-scan")) return Diagnostics.ScanBattery(args);
            using (var mutex = new Mutex(true, "Local\\AirPodsSwitch.Tray", out owns))
            {
                if (!owns) { MessageBox.Show("ProGlide is already running. Click its AirPods icon in the system tray."); return 0; }
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                try { var controller = new Controller(app); app.Run(); controller.Dispose(); return 0; }
                catch (Exception e) { MessageBox.Show(e.Message, "ProGlide could not start"); return 1; }
            }
        }
        static bool owns;
    }
    public sealed class Controller : IDisposable
    {
        readonly Application app;
        readonly Audio audio;
        readonly Forms.NotifyIcon tray;
        readonly DispatcherTimer debounce, poll;
        readonly string folder = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")) ? Path.Combine(AppContext.BaseDirectory, "Data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AirPodsSwitch");
        Preferences preferences = new Preferences();
        HashSet<string> previous = new HashSet<string>();
        bool initialized, disposed;
        Popup popup;
        Window settings;
        Window about;
        readonly System.Drawing.Icon appIcon;
        readonly Action updateMenuTheme;
        readonly BatteryMonitor battery = new BatteryMonitor();
        public Controller(Application application, bool silent = false)
        {
            app = application;
            Directory.CreateDirectory(folder);
            try { using (var f = File.OpenRead(Path.Combine(folder, "settings.xml"))) preferences = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(f); }
            catch (Exception e) { Log("Settings load (using defaults): " + e.Message); }
            Theme.Apply(preferences.ThemeName);
            battery.SetEnabled(preferences.TrackBattery);
            audio = new Audio();
            appIcon = Theme.TrayIcon();
            tray = new Forms.NotifyIcon { Text = "ProGlide · Choose your sound", Icon = appIcon, Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.BackColor = System.Drawing.Color.FromArgb(27, 35, 43); menu.ForeColor = System.Drawing.Color.FromArgb(243, 246, 247);
            menu.ShowImageMargin = false; menu.Font = new System.Drawing.Font("Segoe UI", 10); menu.Padding = new Forms.Padding(6);
            updateMenuTheme = () => { menu.BackColor = Theme.IsLight ? System.Drawing.Color.FromArgb(242, 246, 248) : System.Drawing.Color.FromArgb(27, 35, 43); menu.ForeColor = Theme.IsLight ? System.Drawing.Color.FromArgb(21, 43, 54) : System.Drawing.Color.FromArgb(243, 246, 247); };
            Theme.Changed += updateMenuTheme; updateMenuTheme();
            menu.Items.Add("Choose output", null, (s, e) => Show("Choose your sound"));
            menu.Items.Add("Settings", null, (s, e) => Settings());
            menu.Items.Add("Windows sound settings", null, (s, e) => OpenSound());
            menu.Items.Add("About ProGlide", null, (s, e) => About());
            menu.Items.Add("Exit", null, (s, e) => app.Shutdown());
            tray.ContextMenuStrip = menu;
            tray.MouseClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) Show("Choose your sound"); };
            debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            debounce.Tick += (s, e) => { debounce.Stop(); Refresh(); };
            audio.Changed += QueueRefresh;
            poll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            poll.Tick += (s, e) => Refresh(); poll.Start();
            Refresh();
            if (!silent && !Environment.GetCommandLineArgs().Contains("--startup")) Show("Your sound, one tap away");
        }
        void QueueRefresh()
        {
            if (disposed) return;
            app.Dispatcher.BeginInvoke(new Action(() => { if (!disposed) { debounce.Stop(); debounce.Start(); } }));
        }
        public static bool Matches(Endpoint endpoint, Preferences prefs)
        {
            return !string.IsNullOrEmpty(prefs.AirPodsId) ? endpoint.Id == prefs.AirPodsId :
                !string.IsNullOrWhiteSpace(prefs.Match) && endpoint.Name.IndexOf(prefs.Match, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        void Refresh()
        {
            battery.EnsureScanning();
            try
            {
                var devices = audio.List();
                var current = new HashSet<string>(devices.Where(e => Matches(e, preferences)).Select(e => e.Id));
                bool arrival = initialized && current.Except(previous).Any();
                bool departure = initialized && previous.Count > 0 && current.Count == 0;
                previous = current; initialized = true;
                if (arrival)
                {
                    if (preferences.AutoSwitch) Switch(devices.First(e => current.Contains(e.Id)).Id);
                    Show("AirPods connected");
                }
                else if (departure) Show("AirPods disconnected");
                else if (popup != null && popup.IsVisible) Populate(popup);
            }
            catch (Exception e) { Log("Refresh: " + e); if (popup != null) popup.Status.Text = "Audio service unavailable. Retrying…"; }
        }
        void Switch(string id)
        {
            try { audio.Switch(id); if (popup != null) Populate(popup); }
            catch (Exception e)
            {
                Log("Switch: " + e);
                MessageBox.Show("Could not switch every audio role. Some roles may have changed.\n\n" + e.Message + "\n\nYou can also select an output in Windows Sound settings.", "ProGlide");
            }
        }
        void Show(string title)
        {
            try
            {
                if (popup != null) popup.Close();
                popup = new Popup(title, About);
                popup.Animate = preferences.AnimatePopup;
                Populate(popup);
                var settingsButton = Theme.IconButton("Preferences", "\uE713", () => { popup.Close(); Settings(); }); settingsButton.Margin = new Thickness(0, 0, 8, 0); settingsButton.Width = 160;
                popup.Footer.Children.Add(settingsButton);
                var soundButton = Theme.IconButton("Sound settings", "\uE767", OpenSound); soundButton.Width = 160; soundButton.Margin = new Thickness(0); popup.Footer.Children.Add(soundButton);
                popup.Show();
            }
            catch (Exception e) { Log("Popup: " + e); MessageBox.Show(e.Message, "ProGlide"); }
        }
        void Populate(Popup view)
        {
            var reading = battery.Current();
            view.Battery.Visibility = preferences.TrackBattery ? Visibility.Visible : Visibility.Collapsed;
            view.Battery.Text = reading == null ? battery.Status : "Nearby AirPods battery · " + reading.Display();
            var devices = audio.List(); string active = audio.Default(1);
            var pods = devices.Where(e => Matches(e, preferences)).ToList();
            var speakers = devices.FirstOrDefault(e => e.Id == preferences.SpeakersId);
            string signature = active + "|" + preferences.SpeakersId + "|" + string.Join("|", devices.Select(e => e.Id + e.Name)) + "|" + string.Join("|", pods.Select(e => e.Id));
            if (view.Snapshot == signature) return;
            view.Snapshot = signature;
            view.Outputs.Children.Clear();
            view.SetConnection(pods.Count > 0);
            if (pods.Count == 0) view.Outputs.Children.Add(Theme.Device("AirPods Pro", "Connect your AirPods to use this output", true, false, false, () => { }));
            foreach (var endpoint in pods.Concat(speakers == null ? new Endpoint[0] : new[] { speakers }).GroupBy(e => e.Id).Select(g => g.First()))
            {
                string id = endpoint.Id;
                bool isPods = Matches(endpoint, preferences);
                var button = Theme.Device(isPods ? "AirPods" : "PC speakers", id == active ? "Playing here · " + endpoint.Name : endpoint.Name, isPods, id == active, true, () => Switch(id));
                view.Outputs.Children.Add(button);
            }
            var playing = devices.FirstOrDefault(e => e.Id == active);
            view.Status.Text = pods.Count == 0 ? "Connect your AirPods, or keep listening on your PC." : "Choose where your next moment plays.";
            if (speakers == null) view.Status.Text += string.IsNullOrEmpty(preferences.SpeakersId) ? " Select your speakers in Preferences." : " Your saved speakers are offline.";
            tray.Text = "ProGlide · " + (playing == null ? "No active output" : (Matches(playing, preferences) ? "AirPods" : "PC audio"));
        }
        internal static Button Button(string text, Action click)
        {
            return Theme.Button(text, click, false);
        }
        internal Window PreferencesWindow { get { return settings; } }
        internal ComboBox AppearanceSelector;
        void About()
        {
            if (about != null) { about.Activate(); return; }
            about = new AboutWindow(); about.Closed += (s, e) => about = null; about.Show();
        }
        internal void Settings()
        {
            if (settings != null) { settings.Activate(); return; }
            settings = new Window { Title = "ProGlide · Preferences", Width = 540, Height = Math.Min(880, SystemParameters.WorkArea.Height - 40), MinWidth = 480, MinHeight = 560, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            Theme.Window(settings);
            var stack = new StackPanel { Margin = new Thickness(32, 24, 32, 32) };
            settings.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var brand = new DockPanel(); var art = Theme.Product(66); DockPanel.SetDock(art, Dock.Right); brand.Children.Add(art);
            var heading = new StackPanel(); heading.Children.Add(Theme.Wordmark(15));
            var pageTitle = Theme.Text("Make it yours.", 30, Theme.Ink); pageTitle.FontWeight = FontWeights.SemiBold; pageTitle.Margin = new Thickness(0, 6, 0, 0); heading.Children.Add(pageTitle); brand.Children.Add(heading); stack.Children.Add(brand);
            var intro = Theme.Text("Your devices. Your listening flow.", 13, Theme.Muted); intro.Margin = new Thickness(0, 8, 0, 12); stack.Children.Add(intro);
            Action<string> label = text => stack.Children.Add(new TextBlock { Text = text, Foreground = Theme.Muted, FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 19, 0, 8), TextWrapping = TextWrapping.Wrap });
            List<Endpoint> devices;
            try { devices = audio.List(); } catch (Exception e) { MessageBox.Show(e.Message); settings = null; return; }
            label("AIRPODS OUTPUT");
            var podChoices = new List<Endpoint> { new Endpoint { Id = "", Name = "Detect by name below" } };
            podChoices.AddRange(devices);
            if (!string.IsNullOrEmpty(preferences.AirPodsId) && !devices.Any(d => d.Id == preferences.AirPodsId)) podChoices.Add(new Endpoint { Id = preferences.AirPodsId, Name = "Saved AirPods (currently unavailable)" });
            var pods = new ComboBox { ItemsSource = podChoices, SelectedItem = podChoices.FirstOrDefault(d => d.Id == preferences.AirPodsId) }; Theme.Selector(pods); stack.Children.Add(pods);
            label("DETECT BY NAME");
            var match = new TextBox { Text = preferences.Match, Padding = new Thickness(11, 9, 11, 9), Background = Theme.Surface, Foreground = Theme.Ink, CaretBrush = Theme.Mint, BorderBrush = Theme.Brush("#34434E"), SelectionBrush = Theme.Brush("#385D4F") }; stack.Children.Add(match);
            var hint = Theme.Text("Used only when “Detect by name” is selected above.", 11, Theme.Muted); hint.Margin = new Thickness(0, 6, 0, 0); stack.Children.Add(hint);
            label("PC SPEAKERS");
            var choices = new List<Endpoint> { new Endpoint { Id = "", Name = "Choose an output…" } }; choices.AddRange(devices);
            if (!string.IsNullOrEmpty(preferences.SpeakersId) && !devices.Any(d => d.Id == preferences.SpeakersId)) choices.Add(new Endpoint { Id = preferences.SpeakersId, Name = "Saved speakers (currently unavailable)" });
            var speakers = new ComboBox { ItemsSource = choices, SelectedItem = choices.FirstOrDefault(d => d.Id == preferences.SpeakersId) }; Theme.Selector(speakers); stack.Children.Add(speakers);
            label("LISTENING PREFERENCES");
            var auto = new CheckBox { Content = "Switch to AirPods when they connect", Foreground = Theme.Ink, IsChecked = preferences.AutoSwitch, Margin = new Thickness(0, 4, 0, 6) }; Theme.Toggle(auto); stack.Children.Add(auto);
            var startup = new CheckBox { Content = "Launch quietly when I sign in", Foreground = Theme.Ink, IsChecked = StartupEnabled() }; Theme.Toggle(startup); stack.Children.Add(startup);
            var motion = new CheckBox { Content = "Animate connection popup", Foreground = Theme.Ink, IsChecked = preferences.AnimatePopup, Margin = new Thickness(0, 6, 0, 0), ToolTip = "ProGlide spring animation. Turn off for reduced motion." }; Theme.Toggle(motion); stack.Children.Add(motion);
            label("APPEARANCE");
            var appearance = new ComboBox { ItemsSource = new[] { "Dark", "Light" }, SelectedItem = preferences.ThemeName == "Light" ? "Light" : "Dark" }; Theme.Selector(appearance); stack.Children.Add(appearance);
            AppearanceSelector = appearance;
            appearance.SelectionChanged += (s, e) => { Theme.Apply((string)appearance.SelectedItem); };
            var tracking = new CheckBox { Content = "Read nearby AirPods battery", Foreground = Theme.Ink, IsChecked = preferences.TrackBattery, Margin = new Thickness(0, 14, 0, 0), ToolTip = "Nearby broadcasts report in 10% steps and may belong to another pair. No Bluetooth address is saved." }; Theme.Toggle(tracking); stack.Children.Add(tracking);
            var note = Theme.Text("Changes your playback output, including calls. Disconnecting lets Windows choose a fallback.", 12, Theme.Muted); note.Margin = new Thickness(0, 20, 0, 16); stack.Children.Add(note);
            stack.Children.Add(Theme.Button("Save preferences", () =>
            {
                try
                {
                    var next = new Preferences { AirPodsId = ((Endpoint)pods.SelectedItem).Id, SpeakersId = ((Endpoint)speakers.SelectedItem).Id, Match = match.Text.Trim(), AutoSwitch = auto.IsChecked == true, AnimatePopup = motion.IsChecked == true, ThemeName = (string)appearance.SelectedItem, TrackBattery = tracking.IsChecked == true };
                    if (next.AirPodsId == "" && next.Match == "") throw new InvalidOperationException("Enter an AirPods name to detect.");
                    if (next.AirPodsId != "" && next.AirPodsId == next.SpeakersId) throw new InvalidOperationException("Choose different AirPods and speaker outputs.");
                    SetStartup(startup.IsChecked == true);
                    var path = Path.Combine(folder, "settings.xml");
                    using (var f = File.Create(path + ".tmp")) new XmlSerializer(typeof(Preferences)).Serialize(f, next);
                    if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
                    else File.Move(path + ".tmp", path);
                    preferences = next; battery.SetEnabled(preferences.TrackBattery); initialized = false; Refresh(); settings.Close();
                }
                catch (Exception e) { MessageBox.Show("Could not save settings: " + e.Message); }
            }, true));
            var aboutButton = Theme.IconButton("About ProGlide", "\uE946", About); aboutButton.Margin = new Thickness(0, 10, 0, 0); stack.Children.Add(aboutButton);
            settings.Closed += (s, e) => { Theme.Apply(preferences.ThemeName); settings = null; }; settings.Show();
        }
        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        static bool StartupEnabled() { using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) return key != null && key.GetValue("AirPodsSwitch") != null; }
        static void SetStartup(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled)
                {
                    string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    if (string.IsNullOrEmpty(exe) || Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "ProGlide.exe");
                        if (!File.Exists(exe)) throw new InvalidOperationException("Run ProGlide.exe before enabling startup.");
                    }
                    key.SetValue("AirPodsSwitch", "\"" + exe + "\" --startup");
                }
                else key.DeleteValue("AirPodsSwitch", false);
            }
        }
        static void OpenSound() { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:sound") { UseShellExecute = true }); }
        void Log(string message) { try { File.AppendAllText(Path.Combine(folder, "app.log"), DateTimeOffset.Now + " " + message + Environment.NewLine); } catch { } }
        public void Dispose()
        {
            disposed = true; poll.Stop(); debounce.Stop(); audio.Changed -= QueueRefresh; audio.Dispose();
            Theme.Changed -= updateMenuTheme;
            battery.Dispose();
            tray.Visible = false; tray.Dispose(); appIcon.Dispose(); if (popup != null) popup.Close(); if (settings != null) settings.Close();
            if (about != null) about.Close();
        }
    }
    public sealed class Popup : Window
    {
        public string Snapshot;
        public readonly StackPanel Outputs = new StackPanel();
        public readonly StackPanel Footer = new StackPanel { Orientation = Orientation.Horizontal };
        public readonly TextBlock Connection = new TextBlock { Text = "○  NOT CONNECTED", Foreground = Theme.Muted, FontSize = 10, FontWeight = FontWeights.SemiBold };
        public readonly TextBlock Status = new TextBlock { Foreground = Theme.Muted, FontSize = 13, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(14, 9, 14, 24), LineHeight = 20 };
        public readonly TextBlock Battery = new TextBlock { Foreground = Theme.Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 14), ToolTip = "Nearby broadcasts: device identity is unverified. Levels arrive in 10% steps and expire after 60 seconds. Open the case nearby to refresh." };
        readonly AirPodsCase caseArt = new AirPodsCase();
        Border badge;
        bool connected;
        readonly bool transition;
        readonly bool departure;
        readonly DispatcherTimer timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        readonly TranslateTransform slide = new TranslateTransform();
        readonly ScaleTransform scale = new ScaleTransform(1, 1);
        bool closing;
        public bool Animate = true;
        internal double SlideOffset { get { return slide.Y; } }
        internal double CardScale { get { return scale.ScaleX; } }
        internal double LidProgress { get { return caseArt.OpenProgress; } }
        public void SetConnection(bool value)
        {
            bool changed = connected != value; connected = value;
            Connection.Text = value ? "●  CONNECTED" : departure ? "●  DISCONNECTED" : "●  NOT CONNECTED";
            Connection.Foreground = Theme.Brush(value ? "#79DEA6" : "#FF989F");
            if (badge != null) badge.Background = Theme.Brush(value ? "#202E2A" : "#3F2229");
            if (IsLoaded) caseArt.Present(value, Animate, changed);
        }
        public Popup(string title, Action about = null)
        {
            Title = "ProGlide";
            transition = title == "AirPods connected" || title == "AirPods disconnected";
            departure = title == "AirPods disconnected";
            Theme.Window(this);
            Width = 410; SizeToContent = SizeToContent.Height; MaxHeight = Math.Min(720, SystemParameters.WorkArea.Height - 32);
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; ShowActivated = false; Topmost = true;
            var content = new StackPanel { Margin = new Thickness(28, 20, 28, 22) };
            var header = new DockPanel(); var close = Theme.Button("×", () => Close(), false); close.Width = 28; close.Height = 28; close.Padding = new Thickness(0); close.FontSize = 18; close.ToolTip = "Dismiss"; close.Margin = new Thickness(0); close.Background = Brushes.Transparent; close.BorderThickness = new Thickness(0);
            DockPanel.SetDock(close, Dock.Right); header.Children.Add(close);
            var info = Theme.Button("", () => { if (about != null) about(); }, false); info.Content = Theme.Glyph("\uE946", 15); info.Width = 30; info.Height = 28; info.Padding = new Thickness(0); info.Margin = new Thickness(0, 0, 6, 0); info.BorderThickness = new Thickness(0); info.Background = Brushes.Transparent; info.ToolTip = "About ProGlide"; System.Windows.Automation.AutomationProperties.SetName(info, "About ProGlide"); DockPanel.SetDock(info, Dock.Right); header.Children.Add(info);
            header.Children.Add(Theme.Wordmark(15)); content.Children.Add(header);
            var hero = new Grid { Height = 164, Margin = new Thickness(0, 4, 0, 5) };
            hero.Children.Add(new System.Windows.Shapes.Ellipse { Width = 180, Height = 130, Fill = new RadialGradientBrush(Color.FromArgb(90, 85, 155, 128), Colors.Transparent) });
            var product = new Viewbox { Child = caseArt, Width = 180, Height = 164, Stretch = Stretch.Uniform }; hero.Children.Add(product); content.Children.Add(hero);
            badge = new Border { Child = Connection, HorizontalAlignment = HorizontalAlignment.Center, Background = Theme.Brush("#3F2229"), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 13) }; content.Children.Add(badge);
            SetConnection(title == "AirPods connected");
            var headline = Theme.Text(title, 25, Theme.Ink); headline.FontWeight = FontWeights.SemiBold; headline.TextAlignment = TextAlignment.Center; content.Children.Add(headline);
            content.Children.Add(Status); content.Children.Add(Battery); content.Children.Add(Outputs); content.Children.Add(Footer);
            Footer.Margin = new Thickness(0, 8, 0, 0);
            var background = new LinearGradientBrush(); background.StartPoint = new Point(0, 0); background.EndPoint = new Point(1, 1);
            var topColor = new GradientStop { Offset = 0 }; System.Windows.Data.BindingOperations.SetBinding(topColor, GradientStop.ColorProperty, new System.Windows.Data.Binding("Color") { Source = Theme.Brush("#202B30") }); background.GradientStops.Add(topColor);
            var bottomColor = new GradientStop { Offset = .66 }; System.Windows.Data.BindingOperations.SetBinding(bottomColor, GradientStop.ColorProperty, new System.Windows.Data.Binding("Color") { Source = Theme.Brush("#11161B") }); background.GradientStops.Add(bottomColor);
            var card = new Border { Background = background, BorderBrush = Theme.Brush("#3C4A51"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(24), Child = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
            var transforms = new TransformGroup(); transforms.Children.Add(scale); transforms.Children.Add(slide); card.RenderTransform = transforms; card.RenderTransformOrigin = new Point(.5, 1);
            Content = new Border { Margin = new Thickness(10), Background = Brushes.Transparent, Child = card, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = .3 } };
            Loaded += (s, e) =>
            {
                Position();
                caseArt.Present(connected, Animate, transition);
                if (Animate)
                {
                    // iPhone-inspired spring reveal; this is a 2D Windows animation.
                    BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                    var spring = new BackEase { Amplitude = .18, EasingMode = EasingMode.EaseOut };
                    slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(85, 0, TimeSpan.FromMilliseconds(640)) { EasingFunction = spring });
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.92, 1, TimeSpan.FromMilliseconds(640)) { EasingFunction = spring });
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.92, 1, TimeSpan.FromMilliseconds(640)) { EasingFunction = spring });
                    var floatUp = new TranslateTransform(); product.RenderTransform = floatUp;
                    floatUp.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(850)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                timeout.Start();
            };
            Closing += (s, e) =>
            {
                if (closing || !Animate || !IsVisible || Application.Current.Dispatcher.HasShutdownStarted) return;
                e.Cancel = true; closing = true; timeout.Stop();
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(140)));
                slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(28, TimeSpan.FromMilliseconds(140)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
                var finish = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(155) }; finish.Tick += (sender, args) => { finish.Stop(); Close(); }; finish.Start();
            };
            SizeChanged += (s, e) => Position();
            MouseEnter += (s, e) => timeout.Stop(); MouseLeave += (s, e) => { if (!IsKeyboardFocusWithin) timeout.Start(); };
            GotKeyboardFocus += (s, e) => timeout.Stop(); LostKeyboardFocus += (s, e) => { if (!IsMouseOver) timeout.Start(); };
            KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
            timeout.Tick += (s, e) => Close(); Closed += (s, e) => timeout.Stop();
        }
        void Position() { var area = SystemParameters.WorkArea; Left = Math.Max(area.Left, area.Right - Width - 20); Top = Math.Max(area.Top, area.Bottom - ActualHeight - 20); }
    }
}
