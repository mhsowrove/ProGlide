using System;
using System.Windows;
using System.Windows.Controls;

namespace AirPodsSwitch
{
    public sealed class BatteryView : StackPanel
    {
        readonly TextBlock[] values = new TextBlock[3];
        readonly TextBlock[] details = new TextBlock[3];
        readonly TextBlock caption = Theme.Text("Waiting for a battery reading…", 10, Theme.Muted);
        public BatteryView()
        {
            Margin = new Thickness(0, 0, 0, 13);
            var grid = new Grid();
            var names = new[] { "Left earbud", "Right earbud", "Case" };
            for (int i = 0; i < 3; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                var stack = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
                var name = Theme.Text(names[i], 10, Theme.Muted); name.TextAlignment = TextAlignment.Center; stack.Children.Add(name);
                values[i] = Theme.Text("—", 19, Theme.Muted); values[i].FontWeight = FontWeights.SemiBold; values[i].TextAlignment = TextAlignment.Center; values[i].Margin = new Thickness(0, 3, 0, 1); stack.Children.Add(values[i]);
                details[i] = Theme.Text("No reading", 9, Theme.Muted); details[i].TextAlignment = TextAlignment.Center; stack.Children.Add(details[i]);
                var card = new Border { Background = Theme.Surface, CornerRadius = new CornerRadius(11), BorderBrush = Theme.Brush("#2C3944"), BorderThickness = new Thickness(1), Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 2 ? 0 : 4, 0), Child = stack };
                Grid.SetColumn(card, i); grid.Children.Add(card);
            }
            Children.Add(grid); caption.TextAlignment = TextAlignment.Center; caption.Margin = new Thickness(0, 7, 0, 0); Children.Add(caption);
            ToolTip = "Nearby AirPods broadcasts: device ownership is unverified. Levels arrive in 10% steps. A dash means no recent reading for that component.";
        }
        public void Update(BatteryReading reading, bool enabled, string status)
        {
            var levels = reading == null ? new[] { -1, -1, -1 } : new[] { reading.Left, reading.Right, reading.Case };
            var charging = reading == null ? new[] { false, false, false } : new[] { reading.LeftCharging, reading.RightCharging, reading.CaseCharging };
            var times = reading == null ? new[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue } : new[] { reading.LeftSeen, reading.RightSeen, reading.CaseSeen };
            for (int i = 0; i < 3; i++)
            {
                values[i].Text = levels[i] < 0 ? "—" : levels[i] + "%";
                values[i].Foreground = levels[i] < 0 ? Theme.Muted : levels[i] <= 20 ? Theme.Brush("#FF989F") : Theme.Brush("#79DEA6");
                details[i].Text = levels[i] < 0 ? "No reading" : charging[i] ? "⚡ Charging" : times[i] == DateTime.MinValue ? "Reported" : Math.Max(0, (int)(DateTime.UtcNow - times[i]).TotalSeconds) + "s ago";
            }
            caption.Text = !enabled ? "Battery scanning off · enable in Preferences" : reading == null ? "No recent reading · open the case nearby" : "Nearby AirPods · " + Math.Max(0, (int)(DateTime.UtcNow - reading.Seen).TotalSeconds) + "s ago · device unverified";
            if (enabled && reading == null && status.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0) caption.Text = "Bluetooth battery scanning unavailable";
        }
    }
}
