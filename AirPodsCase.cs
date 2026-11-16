using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AirPodsSwitch
{
    // Frames extracted from Apple's verified AirPods Pro 3 case movie.
    // See ARTWORK-PROVENANCE.md. No product geometry is redrawn or generated.
    public sealed class AirPodsCase : FrameworkElement
    {
        public static readonly DependencyProperty OpenProgressProperty = DependencyProperty.Register("OpenProgress", typeof(double), typeof(AirPodsCase), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public double OpenProgress { get { return (double)GetValue(OpenProgressProperty); } set { SetValue(OpenProgressProperty, value); } }
        const int FrameCount = 49;
        static readonly BitmapImage[] frames = LoadFrames();
        static BitmapImage[] LoadFrames()
        {
            var result = new BitmapImage[FrameCount];
            for (int i = 0; i < FrameCount; i++) result[i] = Theme.Asset("Pro3CaseFrame" + (i + 1).ToString("D3") + ".jpg");
            return result;
        }
        public AirPodsCase() { Width = 200; Height = 180; }
        public void Present(bool connected, bool animate, bool transition)
        {
            double destination = connected ? 1 : 0;
            double from = transition ? (connected ? 0 : 1) : OpenProgress;
            BeginAnimation(OpenProgressProperty, null); OpenProgress = animate && transition ? from : destination;
            if (animate && transition) BeginAnimation(OpenProgressProperty, new DoubleAnimation(from, destination, TimeSpan.FromMilliseconds(1700)) { BeginTime = TimeSpan.FromMilliseconds(160), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
            InvalidateVisual();
        }
        protected override void OnRender(DrawingContext drawing)
        {
            base.OnRender(drawing);
            int index = (int)Math.Round(Math.Max(0, Math.Min(1, OpenProgress)) * (FrameCount - 1));
            var bounds = new Rect(10, 0, 180, 180);
            drawing.PushClip(new RectangleGeometry(bounds, 20, 20));
            drawing.DrawImage(frames[index], bounds); drawing.Pop();
        }
    }
}
