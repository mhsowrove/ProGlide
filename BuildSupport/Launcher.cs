using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
class Launcher
{
    [STAThread] static int Main(string[] args)
    {
        try
        {
            string host = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtime", "dotnet.exe");
            if (!File.Exists(host)) host = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
            string dll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AirPodsSwitch.dll");
            // Only supported UI launch flag is forwarded; use dotnet directly for diagnostics.
            var start = new ProcessStartInfo(host, "\"" + dll + "\"" + (Array.IndexOf(args, "--startup") >= 0 ? " --startup" : ""));
            start.UseShellExecute = false; start.CreateNoWindow = true;
            Process.Start(start); return 0;
        }
        catch (Exception e) { MessageBox.Show("Install the Microsoft .NET 8 Desktop Runtime (x64), then try again.\n\n" + e.Message, "PodGlide"); return 1; }
    }
}
