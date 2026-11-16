using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace AirPodsSwitch
{
    public sealed class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "About ProGlide"; Width = 470; Height = 670; MinWidth = 430; MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen; Theme.Window(this);
            var stack = new StackPanel { Margin = new Thickness(30, 25, 30, 28) };
            Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var art = Theme.Product(94); art.HorizontalAlignment = HorizontalAlignment.Center; stack.Children.Add(art);
            var title = Theme.Text("ProGlide", 32, Theme.Ink); title.FontWeight = FontWeights.SemiBold; title.TextAlignment = TextAlignment.Center; stack.Children.Add(title);
            var subtitle = Theme.Text("Your sound, one tap away.  ·  Version 1.3", 12, Theme.Muted); subtitle.TextAlignment = TextAlignment.Center; subtitle.Margin = new Thickness(0, 6, 0, 23); stack.Children.Add(subtitle);
            var credit = new StackPanel { Margin = new Thickness(16) };
            credit.Children.Add(Theme.Text("DESIGNED & DEVELOPED BY", 10, Theme.Muted));
            var name = Theme.Text("MD SOWROVE HOSSAIN", 17, Theme.Ink); name.FontWeight = FontWeights.SemiBold; name.Margin = new Thickness(0, 7, 0, 0); credit.Children.Add(name);
            stack.Children.Add(new Border { Background = Theme.Surface, CornerRadius = new CornerRadius(13), Child = credit });
            var email = Theme.BrandButton("mhsowrove@gmail.com", "Gmail.png", () => Open("mailto:mhsowrove@gmail.com")); email.Margin = new Thickness(0, 12, 0, 0); stack.Children.Add(email);
            var facebook = Theme.BrandButton("mhsowrove · profile", "Facebook.png", () => Open("https://www.facebook.com/mhsowrove")); facebook.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(facebook);
            var donation = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 22, 0, 10) };
            var heart = Theme.Glyph("\uEB51", 15); heart.Margin = new Thickness(0, 0, 8, 0); donation.Children.Add(heart); donation.Children.Add(Theme.Text("Support ProGlide", 15, Theme.Ink)); stack.Children.Add(donation);
            stack.Children.Add(Theme.Text("If ProGlide makes listening easier, you can support its development.", 12, Theme.Muted));
            var payments = new Grid { Margin = new Thickness(0, 12, 0, 0) }; payments.ColumnDefinitions.Add(new ColumnDefinition()); payments.ColumnDefinitions.Add(new ColumnDefinition());
            var bkash = Payment("bKash", "bKash.png", () =>
            {
                try { Clipboard.SetText("01925070109"); MessageBox.Show("bKash number copied: 01925070109\n\nOpen bKash and check the recipient name before sending a voluntary donation.", "Support ProGlide", MessageBoxButton.OK, MessageBoxImage.Information); }
                catch (Exception e) { MessageBox.Show("bKash: 01925070109\n\n" + e.Message, "Support ProGlide"); }
            });
            bkash.ToolTip = "Copy bKash number: 01925070109"; bkash.Margin = new Thickness(0, 0, 5, 0); payments.Children.Add(bkash);
            var paypal = Payment("PayPal", "PayPal.png", () => Open("https://www.paypal.com/paypalme/mhsowrove")); paypal.Margin = new Thickness(5, 0, 0, 0); Grid.SetColumn(paypal, 1); payments.Children.Add(paypal); stack.Children.Add(payments);
            var number = Theme.Text("bKash · 01925070109", 11, Theme.Muted); number.Margin = new Thickness(0, 9, 0, 0); number.TextAlignment = TextAlignment.Center; stack.Children.Add(number);
            var note = Theme.Text("Independent app. Not affiliated with Apple, bKash or PayPal. Donations are optional and open outside the app.", 10, Theme.Muted); note.Margin = new Thickness(0, 20, 0, 0); note.TextAlignment = TextAlignment.Center; stack.Children.Add(note);
        }
        static Button Payment(string name, string file, Action action)
        {
            var button = Theme.Button("", action, false); button.Padding = new Thickness(12);
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var image = Theme.Asset(file);
            ImageSource logo = name == "bKash" ? (ImageSource)new CroppedBitmap(image, new Int32Rect(300, 85, 250, 240)) : image;
            row.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(6), Width = 36, Height = 32, Padding = new Thickness(4), Margin = new Thickness(0, 0, 9, 0), Child = new Image { Source = logo, Stretch = Stretch.Uniform } });
            row.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center }); button.Content = row;
            System.Windows.Automation.AutomationProperties.SetName(button, "Support with " + name); return button;
        }
        static void Open(string address)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(address) { UseShellExecute = true }); }
            catch (Exception e) { MessageBox.Show("Could not open the link.\n\n" + address + "\n\n" + e.Message, "ProGlide"); }
        }
    }
}
