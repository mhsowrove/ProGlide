// BLE packet layout informed by PodBattery (MIT), hammad20120, and its documented protocol references.
// See THIRD-PARTY-NOTICES.md for attribution and the accompanying license.
using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace AirPodsSwitch
{
    public sealed class BatteryReading
    {
        public int Left = -1, Right = -1, Case = -1;
        public bool LeftCharging, RightCharging, CaseCharging;
        public DateTime Seen;
        public DateTime LeftSeen, RightSeen, CaseSeen;
        public short Signal;
        public string Model;
        static string Value(int value, bool charging) { return value < 0 ? "—" : value + "%" + (charging ? " ⚡" : ""); }
        public string Display() { return "L " + Value(Left, LeftCharging) + "   ·   R " + Value(Right, RightCharging) + "   ·   Case " + Value(Case, CaseCharging); }
    }
    public sealed class BatteryMonitor : IDisposable
    {
        readonly object gate = new object();
        BluetoothLEAdvertisementWatcher watcher;
        BatteryReading latest;
        ulong address;
        volatile bool enabled;
        public string Status = "Waiting for nearby AirPods…";
        public void SetEnabled(bool value)
        {
            enabled = value;
            if (!value) { if (watcher != null) try { watcher.Stop(); } catch { } lock (gate) latest = null; Status = "Battery tracking is off"; return; }
            EnsureScanning();
        }
        public void EnsureScanning()
        {
            if (!enabled) return;
            try
            {
                if (watcher == null) { watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Passive }; watcher.Received += Received; }
                if (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started && watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopping) watcher.Start();
                Status = watcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted ? "Bluetooth battery scanning unavailable" : "Waiting for nearby AirPods…";
            }
            catch { Status = "Bluetooth battery scanning unavailable"; }
        }
        void Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (!enabled) return;
            try
            {
                foreach (var manufacturer in args.Advertisement.GetManufacturerDataByCompanyId(0x004C))
                {
                    var data = new byte[manufacturer.Data.Length]; using (var reader = DataReader.FromBuffer(manufacturer.Data)) reader.ReadBytes(data);
                    var reading = Decode(data, args.RawSignalStrengthInDBm); if (reading == null) continue;
                    lock (gate)
                    {
                        if (!enabled) return;
                        // Broadcast addresses can rotate. This cannot prove ownership of the nearby pair.
                        if (latest != null && address != args.BluetoothAddress && (DateTime.UtcNow - latest.Seen).TotalSeconds < 10 && reading.Signal < latest.Signal + 8) continue;
                        if (latest != null && address == args.BluetoothAddress && latest.Model == reading.Model)
                        {
                            var now = DateTime.UtcNow;
                            if (reading.Left < 0 && (now - latest.LeftSeen).TotalSeconds <= 60) { reading.Left = latest.Left; reading.LeftSeen = latest.LeftSeen; reading.LeftCharging = latest.LeftCharging; }
                            if (reading.Right < 0 && (now - latest.RightSeen).TotalSeconds <= 60) { reading.Right = latest.Right; reading.RightSeen = latest.RightSeen; reading.RightCharging = latest.RightCharging; }
                            if (reading.Case < 0 && (now - latest.CaseSeen).TotalSeconds <= 60) { reading.Case = latest.Case; reading.CaseSeen = latest.CaseSeen; reading.CaseCharging = latest.CaseCharging; }
                        }
                        latest = reading; address = args.BluetoothAddress;
                    }
                }
            }
            catch { }
        }
        public BatteryReading Current()
        {
            lock (gate)
            {
                if (!enabled || latest == null || (DateTime.UtcNow - latest.Seen).TotalSeconds > 60) return null;
                var now = DateTime.UtcNow;
                return new BatteryReading { Model = latest.Model, Signal = latest.Signal, Seen = latest.Seen,
                    Left = (now - latest.LeftSeen).TotalSeconds <= 60 ? latest.Left : -1,
                    Right = (now - latest.RightSeen).TotalSeconds <= 60 ? latest.Right : -1,
                    Case = (now - latest.CaseSeen).TotalSeconds <= 60 ? latest.Case : -1,
                    LeftCharging = latest.LeftCharging, RightCharging = latest.RightCharging, CaseCharging = latest.CaseCharging,
                    LeftSeen = latest.LeftSeen, RightSeen = latest.RightSeen, CaseSeen = latest.CaseSeen };
            }
        }
        public static BatteryReading Decode(byte[] data, short signal)
        {
            if (data == null || data.Length < 27 || data[0] != 7 || data[1] != 25 || signal < -75) return null;
            int model = (data[3] << 8) | data[4];
            string name;
            switch (model)
            {
                case 0x0E20: name = "AirPods Pro"; break;
                case 0x1420: case 0x2420: name = "AirPods Pro 2"; break;
                case 0x2720: name = "AirPods Pro 3"; break;
                case 0x0220: case 0x0F20: case 0x1320: case 0x1920: case 0x1B20: name = "AirPods"; break;
                default: return null;
            }
            bool swapped = (data[5] & 0x20) == 0;
            int first = data[6] >> 4, second = data[6] & 15, charging = data[7] >> 4;
            var result = new BatteryReading {
                Model = name, Left = Percent(swapped ? first : second), Right = Percent(swapped ? second : first), Case = Percent(data[7] & 15),
                LeftCharging = (charging & (swapped ? 2 : 1)) != 0, RightCharging = (charging & (swapped ? 1 : 2)) != 0, CaseCharging = (charging & 4) != 0,
                Seen = DateTime.UtcNow, Signal = signal
            };
            result.LeftSeen = result.Left >= 0 ? result.Seen : DateTime.MinValue;
            result.RightSeen = result.Right >= 0 ? result.Seen : DateTime.MinValue;
            result.CaseSeen = result.Case >= 0 ? result.Seen : DateTime.MinValue;
            return result.Left < 0 && result.Right < 0 && result.Case < 0 ? null : result;
        }
        static int Percent(int value) { return value <= 10 ? value * 10 : -1; }
        public void Dispose() { enabled = false; if (watcher != null) { watcher.Received -= Received; try { watcher.Stop(); } catch { } } }
    }
}
