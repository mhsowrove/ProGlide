using System;
using System.IO;
using System.Linq;
using System.Text;
namespace AirPodsSwitch
{
    public static class Diagnostics
    {
        public static int ScanBattery(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var report = new StringBuilder(); var app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            var sample = new byte[27]; sample[0] = 7; sample[1] = 25; sample[3] = 0x27; sample[4] = 0x20; sample[6] = 0x87; sample[7] = 0x49;
            var decoded = BatteryMonitor.Decode(sample, -40);
            if (decoded == null || decoded.Left != 80 || decoded.Right != 70 || decoded.Case != 90 || !decoded.CaseCharging) return 1;
            sample[5] = 0x20; decoded = BatteryMonitor.Decode(sample, -40);
            if (decoded == null || decoded.Left != 70 || decoded.Right != 80 || BatteryMonitor.Decode(new byte[2], -40) != null || BatteryMonitor.Decode(sample, -90) != null) return 1;
            report.AppendLine("PASS: battery packet decoding, left/right ordering, charging flag, malformed and distant-packet rejection.");
            using (var monitor = new BatteryMonitor())
            {
                monitor.SetEnabled(true); int seconds = 0; BatteryReading last = null;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (s, e) => { monitor.EnsureScanning(); if (monitor.Current() != null) last = monitor.Current(); if (++seconds < 20) return; timer.Stop(); report.AppendLine("Scanner status: " + monitor.Status); report.AppendLine(last == null ? "No live nearby AirPods battery packet observed during 20-second test. Hardware readings remain unverified." : "LIVE NEARBY READING (device identity unverified): " + last.Model + " · " + last.Display()); app.Shutdown(); };
                timer.Start(); app.Run();
            }
            File.WriteAllText(args[index + 1], report.ToString()); return 0;
        }
        public static int TestAnimation(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var report = new StringBuilder(); int result = 0;
            bool disconnect = args.Contains("--disconnect");
            var app = new System.Windows.Application(); var view = new Popup(disconnect ? "AirPods disconnected" : "AirPods connected"); view.SetConnection(!disconnect);
            int stage = 0; var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            timer.Tick += (s, e) =>
            {
                if (stage == 0)
                {
                    report.AppendLine("Windows client animations: " + System.Windows.SystemParameters.ClientAreaAnimation + "; ProGlide has its own animation preference.");
                    report.AppendLine("Initial animation sample: offset=" + view.SlideOffset + ", scale=" + view.CardScale + ", opacity=" + view.Opacity);
                    report.AppendLine("Lid intermediate progress: " + view.LidProgress + "; target: " + (disconnect ? "closed" : "open"));
                    stage = 1;
                }
                if (stage == 1 && elapsed.ElapsedMilliseconds >= 550)
                {
                    report.AppendLine("Lid moving progress: " + view.LidProgress);
                    if (view.LidProgress <= .01 || view.LidProgress >= .99) { report.AppendLine("FAIL: no intermediate lid motion."); result = 1; }
                    stage = 2;
                }
                if (elapsed.ElapsedMilliseconds < 2150) return;
                if (Math.Abs(view.SlideOffset) > .01 || Math.Abs(view.CardScale - 1) > .01 || Math.Abs(view.Opacity - 1) > .01) { report.AppendLine("FAIL: reveal did not settle."); result = 1; }
                else report.AppendLine("PASS: spring reveal settled at its final position, scale and opacity.");
                if (Math.Abs(view.LidProgress - (disconnect ? 0 : 1)) > .01) { report.AppendLine("FAIL: lid did not reach its state."); result = 1; }
                else report.AppendLine("PASS: lid reached " + (disconnect ? "closed" : "open") + " state; badge: " + view.Connection.Text);
                timer.Stop(); view.Animate = false; view.Close();
            };
            view.Loaded += (s, e) => { elapsed.Restart(); timer.Start(); };
            app.Run(view); File.WriteAllText(args[index + 1], report.ToString()); return result;
        }
        public static int RenderAbout(string[] args)
        {
            int index = Array.IndexOf(args, "--report"); if (index < 0 || index + 1 >= args.Length) return 2;
            var app = new System.Windows.Application(); Theme.Apply(args.Contains("--light") ? "Light" : "Dark"); var view = new AboutWindow(); view.Show();
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
                if (args.Contains("--cycle-themes")) { controller.AppearanceSelector.SelectedItem = "Light"; controller.AppearanceSelector.SelectedItem = "Dark"; }
                if (args.Contains("--light")) controller.AppearanceSelector.SelectedItem = "Light";
                if (args.Contains("--dark")) controller.AppearanceSelector.SelectedItem = "Dark";
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
            Theme.Apply(args.Contains("--light") ? "Light" : "Dark");
            bool connected = args.Contains("--connected-sample");
            var view = new Popup(connected ? "AirPods connected" : args.Contains("--disconnected-sample") ? "AirPods disconnected" : "Your sound, one tap away");
            view.Animate = false;
            view.Status.Text = connected ? "Choose where your next moment plays." : "Connect your AirPods, or keep listening on your PC.";
            view.SetConnection(connected);
            view.Battery.Text = "Battery · Open the case nearby to refresh";
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
