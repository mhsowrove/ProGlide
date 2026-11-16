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
    }
    public static class Program
    {
        [STAThread] public static int Main(string[] args)
        {
            if (args.Contains("--diagnose")) return Diagnostics.Run(args);
            if (args.Contains("--render-test")) return Diagnostics.Render(args);
            if (args.Contains("--render-preferences")) return Diagnostics.RenderPreferences(args);
            using (var mutex = new Mutex(true, "Local\\AirPodsSwitch.Tray", out owns))
            {
                if (!owns) { MessageBox.Show("PodGlide is already running. Click its AirPods icon in the system tray."); return 0; }
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                try { var controller = new Controller(app); app.Run(); controller.Dispose(); return 0; }
                catch (Exception e) { MessageBox.Show(e.Message, "PodGlide could not start"); return 1; }
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
        readonly System.Drawing.Icon appIcon;
        public Controller(Application application, bool silent = false)
        {
            app = application;
            Directory.CreateDirectory(folder);
            try { using (var f = File.OpenRead(Path.Combine(folder, "settings.xml"))) preferences = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(f); }
            catch (Exception e) { Log("Settings load (using defaults): " + e.Message); }
            audio = new Audio();
            appIcon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "PodGlide.ico"));
            tray = new Forms.NotifyIcon { Text = "PodGlide · Choose your sound", Icon = appIcon, Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.BackColor = System.Drawing.Color.FromArgb(27, 35, 43); menu.ForeColor = System.Drawing.Color.FromArgb(243, 246, 247);
            menu.ShowImageMargin = false; menu.Font = new System.Drawing.Font("Segoe UI", 10); menu.Padding = new Forms.Padding(6);
            menu.Items.Add("Choose output", null, (s, e) => Show("Choose your sound"));
            menu.Items.Add("Settings", null, (s, e) => Settings());
            menu.Items.Add("Windows sound settings", null, (s, e) => OpenSound());
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
                MessageBox.Show("Could not switch every audio role. Some roles may have changed.\n\n" + e.Message + "\n\nYou can also select an output in Windows Sound settings.", "PodGlide");
            }
        }
        void Show(string title)
        {
            try
            {
                if (popup != null) popup.Close();
                popup = new Popup(title);
                Populate(popup);
                var settingsButton = Button("Preferences", () => { popup.Close(); Settings(); }); settingsButton.Margin = new Thickness(0, 0, 8, 0); settingsButton.Width = 160;
                popup.Footer.Children.Add(settingsButton);
                var soundButton = Button("Sound settings ↗", OpenSound); soundButton.Width = 160; soundButton.Margin = new Thickness(0); popup.Footer.Children.Add(soundButton);
                popup.Show();
            }
            catch (Exception e) { Log("Popup: " + e); MessageBox.Show(e.Message, "PodGlide"); }
        }
        void Populate(Popup view)
        {
            var devices = audio.List(); string active = audio.Default(1);
            var pods = devices.Where(e => Matches(e, preferences)).ToList();
            var speakers = devices.FirstOrDefault(e => e.Id == preferences.SpeakersId);
            string signature = active + "|" + preferences.SpeakersId + "|" + string.Join("|", devices.Select(e => e.Id + e.Name)) + "|" + string.Join("|", pods.Select(e => e.Id));
            if (view.Snapshot == signature) return;
            view.Snapshot = signature;
            view.Outputs.Children.Clear();
            view.Connection.Text = pods.Count > 0 ? "●  CONNECTED" : "○  NOT CONNECTED";
            view.Connection.Foreground = pods.Count > 0 ? Theme.Mint : Theme.Muted;
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
            tray.Text = "PodGlide · " + (playing == null ? "No active output" : (Matches(playing, preferences) ? "AirPods" : "PC audio"));
        }
        internal static Button Button(string text, Action click)
        {
            return Theme.Button(text, click, false);
        }
        internal Window PreferencesWindow { get { return settings; } }
        internal void Settings()
        {
            if (settings != null) { settings.Activate(); return; }
            settings = new Window { Title = "PodGlide · Preferences", Width = 540, Height = 730, MinWidth = 480, MinHeight = 560, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            Theme.Window(settings);
            var stack = new StackPanel { Margin = new Thickness(32, 24, 32, 32) };
            settings.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var brand = new DockPanel(); var art = Theme.Product(66); DockPanel.SetDock(art, Dock.Right); brand.Children.Add(art);
            var heading = new StackPanel(); heading.Children.Add(Theme.Text("PODGLIDE", 11, Theme.Mint));
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
            var note = Theme.Text("Changes your playback output, including calls. Disconnecting lets Windows choose a fallback.", 12, Theme.Muted); note.Margin = new Thickness(0, 20, 0, 16); stack.Children.Add(note);
            stack.Children.Add(Theme.Button("Save preferences", () =>
            {
                try
                {
                    var next = new Preferences { AirPodsId = ((Endpoint)pods.SelectedItem).Id, SpeakersId = ((Endpoint)speakers.SelectedItem).Id, Match = match.Text.Trim(), AutoSwitch = auto.IsChecked == true };
                    if (next.AirPodsId == "" && next.Match == "") throw new InvalidOperationException("Enter an AirPods name to detect.");
                    if (next.AirPodsId != "" && next.AirPodsId == next.SpeakersId) throw new InvalidOperationException("Choose different AirPods and speaker outputs.");
                    SetStartup(startup.IsChecked == true);
                    var path = Path.Combine(folder, "settings.xml");
                    using (var f = File.Create(path + ".tmp")) new XmlSerializer(typeof(Preferences)).Serialize(f, next);
                    File.Move(path + ".tmp", path, true);
                    preferences = next; initialized = false; Refresh(); settings.Close();
                }
                catch (Exception e) { MessageBox.Show("Could not save settings: " + e.Message); }
            }, true));
            settings.Closed += (s, e) => settings = null; settings.Show();
        }
        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        static bool StartupEnabled() { using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) return key != null && key.GetValue("AirPodsSwitch") != null; }
        static void SetStartup(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled)
                {
                    string exe = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exe) || Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "PodGlide.exe");
                        if (!File.Exists(exe)) throw new InvalidOperationException("Run the published AirPodsSwitch.exe before enabling startup.");
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
            tray.Visible = false; tray.Dispose(); appIcon.Dispose(); if (popup != null) popup.Close(); if (settings != null) settings.Close();
        }
    }
    public sealed class Popup : Window
    {
        public string Snapshot;
        public readonly StackPanel Outputs = new StackPanel();
        public readonly StackPanel Footer = new StackPanel { Orientation = Orientation.Horizontal };
        public readonly TextBlock Connection = new TextBlock { Text = "○  NOT CONNECTED", Foreground = Theme.Muted, FontSize = 10, FontWeight = FontWeights.SemiBold };
        public readonly TextBlock Status = new TextBlock { Foreground = Theme.Muted, FontSize = 13, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(14, 9, 14, 24), LineHeight = 20 };
        readonly DispatcherTimer timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        public Popup(string title)
        {
            Title = "PodGlide";
            Theme.Window(this);
            Width = 410; SizeToContent = SizeToContent.Height; MaxHeight = Math.Min(720, SystemParameters.WorkArea.Height - 32);
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; ShowActivated = false; Topmost = true;
            var content = new StackPanel { Margin = new Thickness(28, 20, 28, 22) };
            var header = new DockPanel(); var close = Theme.Button("×", () => Close(), false); close.Width = 28; close.Height = 28; close.Padding = new Thickness(0); close.FontSize = 18; close.ToolTip = "Dismiss"; close.Margin = new Thickness(0); close.Background = Brushes.Transparent; close.BorderThickness = new Thickness(0);
            DockPanel.SetDock(close, Dock.Right); header.Children.Add(close);
            var wordmark = Theme.Text("PODGLIDE", 11, Theme.Muted); wordmark.FontWeight = FontWeights.SemiBold; wordmark.VerticalAlignment = VerticalAlignment.Center; header.Children.Add(wordmark); content.Children.Add(header);
            var hero = new Grid { Height = 164, Margin = new Thickness(0, 4, 0, 5) };
            hero.Children.Add(new System.Windows.Shapes.Ellipse { Width = 180, Height = 130, Fill = new RadialGradientBrush(Color.FromArgb(90, 85, 155, 128), Colors.Transparent) });
            var product = Theme.Product(154); product.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 9, Opacity = .26 }; hero.Children.Add(product); content.Children.Add(hero);
            var badge = new Border { Child = Connection, HorizontalAlignment = HorizontalAlignment.Center, Background = Theme.Brush("#202E2A"), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 13) }; content.Children.Add(badge);
            var headline = Theme.Text(title, 25, Theme.Ink); headline.FontWeight = FontWeights.SemiBold; headline.TextAlignment = TextAlignment.Center; content.Children.Add(headline);
            content.Children.Add(Status); content.Children.Add(Outputs); content.Children.Add(Footer);
            Footer.Margin = new Thickness(0, 8, 0, 0);
            var background = new LinearGradientBrush(); background.StartPoint = new Point(0, 0); background.EndPoint = new Point(1, 1); background.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#202B30"), 0)); background.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#11161B"), .66));
            var card = new Border { Background = background, BorderBrush = Theme.Brush("#3C4A51"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(24), Child = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
            Content = new Border { Margin = new Thickness(10), Background = Brushes.Transparent, Child = card, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = .3 } };
            Loaded += (s, e) => { Position(); if (SystemParameters.ClientAreaAnimation) BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))); timeout.Start(); };
            SizeChanged += (s, e) => Position();
            MouseEnter += (s, e) => timeout.Stop(); MouseLeave += (s, e) => { if (!IsKeyboardFocusWithin) timeout.Start(); };
            GotKeyboardFocus += (s, e) => timeout.Stop(); LostKeyboardFocus += (s, e) => { if (!IsMouseOver) timeout.Start(); };
            KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
            timeout.Tick += (s, e) => Close(); Closed += (s, e) => timeout.Stop();
        }
        void Position() { var area = SystemParameters.WorkArea; Left = Math.Max(area.Left, area.Right - Width - 20); Top = Math.Max(area.Top, area.Bottom - ActualHeight - 20); }
    }
}
