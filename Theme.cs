using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AirPodsSwitch
{
    internal static class Theme
    {
        static readonly Dictionary<string, SolidColorBrush> colors = new Dictionary<string, SolidColorBrush>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, PaletteColor> states = new Dictionary<string, PaletteColor>(StringComparer.OrdinalIgnoreCase);
        sealed class PaletteColor : DependencyObject
        {
            public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(PaletteColor));
            public Color Color { get { return (Color)GetValue(ColorProperty); } set { SetValue(ColorProperty, value); } }
        }
        static readonly Dictionary<string, string> light = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            {"#11161B", "#F2F6F8"}, {"#1B232B", "#FFFFFF"}, {"#F3F6F7", "#152B36"}, {"#99A8B4", "#526975"}, {"#9BE4C5", "#147A54"}, {"#11251D", "#FFFFFF"},
            {"#303E49", "#C8D6DE"}, {"#34434E", "#C7D5DC"}, {"#42564D", "#91B6A7"}, {"#30473E", "#DFEEE7"}, {"#193228", "#FFFFFF"}, {"#52616D", "#8FA4AF"},
            {"#BBC7CF", "#FFFFFF"}, {"#20372F", "#E4F3EB"}, {"#6DBB9B", "#3B9872"}, {"#2C3944", "#D1DDE3"}, {"#B1D9C7", "#32664F"}, {"#202E2A", "#DCEFE4"},
            {"#3C4A51", "#CCD9E0"}, {"#385D4F", "#BEDCCD"}, {"#202B30", "#FFFFFF"}, {"#79DEA6", "#117943"}, {"#FF989F", "#B5273B"}, {"#3F2229", "#FCE4E8"}
        };
        public static bool IsLight;
        public static event Action Changed;
        public static readonly Brush Background = Brush("#11161B"), Surface = Brush("#1B232B"), Ink = Brush("#F3F6F7"), Muted = Brush("#99A8B4"), Mint = Brush("#9BE4C5");
        public static SolidColorBrush Brush(string hex)
        {
            SolidColorBrush brush;
            if (!colors.TryGetValue(hex, out brush))
            {
                var state = new PaletteColor { Color = (Color)ColorConverter.ConvertFromString(IsLight && light.ContainsKey(hex) ? light[hex] : hex) };
                brush = new SolidColorBrush();
                System.Windows.Data.BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new System.Windows.Data.Binding("Color") { Source = state });
                states[hex] = state; colors[hex] = brush;
            }
            return brush;
        }
        public static void Apply(string name)
        {
            IsLight = name == "Light";
            foreach (var entry in states) entry.Value.Color = (Color)ColorConverter.ConvertFromString(IsLight && light.ContainsKey(entry.Key) ? light[entry.Key] : entry.Key);
            var changed = Changed; if (changed != null) changed();
        }
        static ControlTemplate Template(string xaml)
        {
            xaml = Regex.Replace(xaml, "(Background|Foreground|Fill|Stroke|BorderBrush|Value)='(#[A-Fa-f0-9]+)'", match => {
                string key = "Palette" + match.Groups[2].Value.Substring(1);
                Application.Current.Resources[key] = Brush(match.Groups[2].Value);
                return match.Groups[1].Value + "='{DynamicResource " + key + "}'";
            });
            return (ControlTemplate)XamlReader.Parse(xaml);
        }
        static readonly ControlTemplate ButtonTemplate = Template(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
            "<Border x:Name='Shell' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='12' Padding='{TemplateBinding Padding}'>" +
            "<ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border>" +
            "<ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Shell' Property='Opacity' Value='.8'/></Trigger>" +
            "<Trigger Property='IsPressed' Value='True'><Setter TargetName='Shell' Property='Opacity' Value='.6'/></Trigger>" +
            "<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Shell' Property='BorderBrush' Value='#9BE4C5'/><Setter TargetName='Shell' Property='BorderThickness' Value='2'/></Trigger>" +
            "<Trigger Property='IsEnabled' Value='False'><Setter TargetName='Shell' Property='Opacity' Value='.42'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        public static Button Button(string text, Action action, bool primary)
        {
            var button = new Button { Content = text, Template = ButtonTemplate, Background = primary ? Mint : Surface, Foreground = primary ? Brush("#11251D") : Ink,
                BorderBrush = Brush("#303E49"), BorderThickness = new Thickness(primary ? 0 : 1), Padding = new Thickness(15, 11, 15, 11), Margin = new Thickness(0, 4, 0, 0),
                FontSize = 13, FontWeight = FontWeights.SemiBold, Cursor = System.Windows.Input.Cursors.Hand, HorizontalContentAlignment = HorizontalAlignment.Center };
            button.Click += (s, e) => action(); return button;
        }
        public static TextBlock Text(string text, double size, Brush foreground)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = foreground, TextWrapping = TextWrapping.Wrap };
        }
        public static TextBlock Glyph(string code, double size)
        {
            return new TextBlock { Text = code, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = size, VerticalAlignment = VerticalAlignment.Center, Foreground = Mint };
        }
        public static Button IconButton(string text, string glyph, Action action)
        {
            var button = Button(text, action, false);
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = Glyph(glyph, 15); icon.Margin = new Thickness(0, 0, 9, 0); panel.Children.Add(icon);
            panel.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }); button.Content = panel;
            System.Windows.Automation.AutomationProperties.SetName(button, text); return button;
        }
        public static Button BrandButton(string text, string file, Action action)
        {
            var button = Button(text, action, false); var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new Image { Source = Asset(file), Width = 22, Height = 22, Margin = new Thickness(0, 0, 10, 0), Stretch = Stretch.Uniform });
            row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }); button.Content = row; return button;
        }
        public static BitmapImage Asset(string file)
        {
            using (var stream = Resource(file))
            {
                var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
            }
        }
        static Stream Resource(string file)
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ProGlide.Assets." + file) ?? File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", file));
        }
        public static System.Drawing.Icon TrayIcon()
        {
            using (var stream = Resource("PodGlide.ico"))
            using (var icon = new System.Drawing.Icon(stream)) return (System.Drawing.Icon)icon.Clone();
        }
        public static StackPanel Wordmark(double size)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var art = Product(size + 10); art.Margin = new Thickness(0, 0, 8, 0); row.Children.Add(art);
            var name = Text("ProGlide", size, Ink); name.FontWeight = FontWeights.SemiBold; name.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(name); return row;
        }
        public static BitmapImage ProductImage()
        {
            return Asset("AirPodsPro3.png");
        }
        public static Image Product(double size) { return new Image { Source = ProductImage(), Width = size, Height = size, Stretch = Stretch.Uniform }; }
        public static void Selector(ComboBox combo)
        {
            combo.Foreground = Ink; combo.Background = Surface; combo.BorderBrush = Brush("#34434E"); combo.FontSize = 13;
            combo.Template = Template(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'>" +
                "<Grid><ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}'>" +
                "<ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='#1B232B' BorderBrush='#34434E' BorderThickness='1' CornerRadius='10'/></ControlTemplate></ToggleButton.Template></ToggleButton>" +
                "<DockPanel Margin='12,11' IsHitTestVisible='False'><Path DockPanel.Dock='Right' Data='M 0 0 L 5 5 L 10 0' Stroke='#9BE4C5' StrokeThickness='1.6' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Width='11' Height='7' Stretch='Uniform' VerticalAlignment='Center' Margin='12,0,0,0'/>" +
                "<ContentPresenter Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' VerticalAlignment='Center'/></DockPanel>" +
                "<Popup x:Name='PART_Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>" +
                "<Border Background='#1B232B' BorderBrush='#42564D' BorderThickness='1' CornerRadius='10' Padding='6' Margin='0,4,0,0' MinWidth='{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}'>" +
                "<ScrollViewer MaxHeight='240' CanContentScroll='True'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer></Border></Popup></Grid>" +
                "<ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            var style = new Style(typeof(ComboBoxItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, Ink));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 9, 10, 9)));
            style.Setters.Add(new Setter(Control.TemplateProperty, Template(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBoxItem'><Border x:Name='Row' Background='Transparent' CornerRadius='6' Padding='{TemplateBinding Padding}'><ContentPresenter/></Border><ControlTemplate.Triggers>" +
                "<Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Row' Property='Background' Value='#30473E'/></Trigger><Trigger Property='IsSelected' Value='True'><Setter Property='Foreground' Value='#9BE4C5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
            combo.ItemContainerStyle = style;
        }
        public static void Toggle(CheckBox check)
        {
            check.Template = Template(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='44'/></Grid.ColumnDefinitions>" +
                "<ContentPresenter VerticalAlignment='Center' Margin='0,8,10,8'/><Border x:Name='Track' Grid.Column='1' Height='23' Width='42' CornerRadius='12' Background='#34434E' BorderBrush='#52616D' BorderThickness='1'>" +
                "<Ellipse x:Name='Thumb' Fill='#BBC7CF' Width='15' Height='15' HorizontalAlignment='Left' Margin='3,0'/></Border></Grid><ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'><Setter TargetName='Track' Property='Background' Value='#9BE4C5'/><Setter TargetName='Track' Property='BorderBrush' Value='#9BE4C5'/><Setter TargetName='Thumb' Property='Fill' Value='#193228'/><Setter TargetName='Thumb' Property='HorizontalAlignment' Value='Right'/></Trigger>" +
                "<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Track' Property='BorderBrush' Value='White'/><Setter TargetName='Track' Property='BorderThickness' Value='2'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        }
        public static Button Device(string name, string subtitle, bool airpods, bool selected, bool available, Action action)
        {
            var button = Button("", action, false);
            button.Margin = new Thickness(0, 0, 0, 10); button.Padding = new Thickness(14); button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Background = selected ? Brush("#20372F") : Surface;
            button.BorderBrush = selected ? Brush("#6DBB9B") : Brush("#2C3944"); button.IsEnabled = available;
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            FrameworkElement icon;
            if (airpods) icon = Product(34);
            else icon = new TextBlock { Text = "\uE767", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 24, Foreground = selected ? Mint : Muted, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(icon);
            var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) }; Grid.SetColumn(copy, 1);
            var label = Text(name, 14, Ink); label.FontWeight = FontWeights.SemiBold; copy.Children.Add(label);
            var detail = new TextBlock { Text = subtitle, FontSize = 11, Foreground = selected ? Brush("#B1D9C7") : Muted, Margin = new Thickness(0, 5, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = subtitle }; copy.Children.Add(detail); grid.Children.Add(copy);
            var marker = Text(selected ? "✓" : "›", selected ? 17 : 24, selected ? Mint : Muted); marker.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(marker, 2); grid.Children.Add(marker);
            button.Content = grid; button.ToolTip = subtitle; System.Windows.Automation.AutomationProperties.SetName(button, name + ". " + subtitle); return button;
        }
        public static void Window(Window window)
        {
            window.FontFamily = new FontFamily("Segoe UI"); window.FontSize = 14; window.Foreground = Ink; window.Background = Background;
            using (var stream = Resource("PodGlide.ico")) window.Icon = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            window.Resources[SystemColors.WindowBrushKey] = Surface;
            window.Resources[SystemColors.WindowTextBrushKey] = Ink;
            window.Resources[SystemColors.ControlBrushKey] = Surface;
            window.Resources[SystemColors.ControlTextBrushKey] = Ink;
            window.Resources[SystemColors.HighlightBrushKey] = Brush("#385D4F");
            window.Resources[SystemColors.HighlightTextBrushKey] = Ink;
            TextOptions.SetTextFormattingMode(window, TextFormattingMode.Display);
        }
    }
}
