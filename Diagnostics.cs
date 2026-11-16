using System;
using System.IO;
using System.Linq;
using System.Text;
namespace AirPodsSwitch
{
    public static class Diagnostics
    {
        public static int TestAnimation(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var report = new StringBuilder(); int result = 0;
            var app = new System.Windows.Application(); var view = new Popup("AirPods connected");
            int stage = 0; var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            timer.Tick += (s, e) =>
            {
                if (stage == 0)
                {
                    report.AppendLine("Windows client animations: " + System.Windows.SystemParameters.ClientAreaAnimation + "; PodGlide has its own animation preference.");
                    report.AppendLine("Initial animation sample: offset=" + view.SlideOffset + ", scale=" + view.CardScale + ", opacity=" + view.Opacity);
                    stage = 1;
                }
                if (elapsed.ElapsedMilliseconds < 1200) return;
                if (Math.Abs(view.SlideOffset) > .01 || Math.Abs(view.CardScale - 1) > .01 || Math.Abs(view.Opacity - 1) > .01) { report.AppendLine("FAIL: reveal did not settle."); result = 1; }
                else report.AppendLine("PASS: spring reveal settled at its final position, scale and opacity.");
                timer.Stop(); view.Animate = false; view.Close();
            };
            view.Loaded += (s, e) => { elapsed.Restart(); timer.Start(); };
            app.Run(view); File.WriteAllText(args[index + 1], report.ToString()); return result;
        }
        public static int RenderAbout(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var app = new System.Windows.Application(); var view = new AboutWindow(); view.Show();
            app.Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Render); view.UpdateLayout();
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth), (int)Math.Ceiling(view.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(view); var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using (var f = File.Create(args[index + 1])) encoder.Save(f); view.Close(); app.Shutdown(); return 0;
        }
        public static int RenderPreferences(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            using (var controller = new Controller(app, true))
            {
                controller.Settings();
                var view = controller.PreferencesWindow;
                app.Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Render);
                view.UpdateLayout();
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth), (int)Math.Ceiling(view.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(view); var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var f = File.Create(args[index + 1])) encoder.Save(f);
            }
            app.Shutdown(); return 0;
        }
        public static int Render(string[] args)
        {
            int index = Array.IndexOf(args, "--report");
            if (index < 0 || index + 1 >= args.Length) return 2;
            var app = new System.Windows.Application();
            bool connected = args.Contains("--connected-sample");
            var view = new Popup(connected ? "AirPods connected" : "Your sound, one tap away");
            view.Animate = false;
            view.Status.Text = connected ? "Choose where your next moment plays." : "Connect your AirPods, or keep listening on your PC.";
            view.Connection.Text = connected ? "●  CONNECTED" : "○  NOT CONNECTED";
            view.Connection.Foreground = connected ? Theme.Mint : Theme.Muted;
            view.Outputs.Children.Add(Theme.Device("AirPods", connected ? "Playing here · AirPods Pro" : "Connect your AirPods to use this output", true, connected, connected, () => { }));
            view.Outputs.Children.Add(Theme.Device("PC speakers", connected ? "Speakers (Realtek Audio)" : "Playing here · Speakers (Realtek Audio)", false, !connected, true, () => { }));
            var preferences = Theme.IconButton("Preferences", "\uE713", () => { }); preferences.Width = 160; preferences.Margin = new System.Windows.Thickness(0, 0, 8, 0); view.Footer.Children.Add(preferences);
            var sound = Theme.IconButton("Sound settings", "\uE767", () => { }); sound.Width = 160; sound.Margin = new System.Windows.Thickness(0); view.Footer.Children.Add(sound);
            view.Show();
            app.Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Render);
            view.BeginAnimation(System.Windows.UIElement.OpacityProperty, null); view.Opacity = 1;
            view.UpdateLayout();
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth), (int)Math.Ceiling(view.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(view);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using (var f = File.Create(args[index + 1])) encoder.Save(f);
            view.Close(); app.Shutdown(); return 0;
        }
        public static int Run(string[] args)
        {
            var report = new StringBuilder(); int result = 0;
            try
            {
                report.AppendLine(args.Contains("--test-current-switch") ? "AirPods Switch diagnostics including current-output switch test" : "AirPods Switch read-only diagnostics");
                report.AppendLine("OS: " + Environment.OSVersion + "; runtime: " + Environment.Version);
                var preferences = new Preferences();
                if (!Controller.Matches(new Endpoint { Id = "a", Name = "Headphones (AIRPODS Pro)" }, preferences)) throw new Exception("Name match test failed");
                preferences.AirPodsId = "b";
                if (Controller.Matches(new Endpoint { Id = "a", Name = "AirPods" }, preferences)) throw new Exception("Saved endpoint precedence test failed");
                if (!Controller.Matches(new Endpoint { Id = "b", Name = "Renamed headphones" }, preferences)) throw new Exception("Saved endpoint match test failed");
                report.AppendLine("PASS: detection name, case-insensitive matching, saved ID precedence, renamed endpoint.");
                using (var audio = new Audio())
                {
                    var devices = audio.List(); report.AppendLine("Active playback endpoints: " + devices.Count);
                    foreach (var d in devices) report.AppendLine(d.Name + " | " + d.Id);
                    for (int role = 0; role < 3; role++) report.AppendLine("Default role " + role + ": " + (audio.Default(role) ?? "none"));
                    try { audio.Switch("__invalid_test_endpoint__"); throw new Exception("Invalid endpoint was accepted"); }
                    catch (InvalidOperationException) { report.AppendLine("PASS: invalid switch rejected before changing Windows settings."); }
                    // Instantiate and query the private interface without changing any endpoint.
                    object policy = new PolicyClass();
                    try { if (!(policy is IPolicy)) throw new Exception("PolicyConfig interface unavailable"); report.AppendLine("PASS: PolicyConfig interface available (no switch performed)."); }
                    finally { System.Runtime.InteropServices.Marshal.ReleaseComObject(policy); }
                    if (args.Contains("--test-current-switch"))
                    {
                        string current = audio.Default(0);
                        if (current == null || audio.Default(1) != current || audio.Default(2) != current)
                            throw new InvalidOperationException("Switch test requires all three roles to already use the same endpoint.");
                        audio.Switch(current);
                        report.AppendLine("PASS: SetDefaultEndpoint succeeded for all three roles, re-selecting the existing output (routing between distinct devices remains untested).");
                    }
                }
            }
            catch (Exception e) { report.AppendLine("FAIL: " + e); result = 1; }
            int index = Array.IndexOf(args, "--report");
            if (index >= 0 && index + 1 < args.Length) File.WriteAllText(args[index + 1], report.ToString());
            else Console.WriteLine(report.ToString());
            return result;
        }
    }
}
