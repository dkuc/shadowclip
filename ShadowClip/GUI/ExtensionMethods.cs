using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaElement = Unosquare.FFME.MediaElement;

namespace ShadowClip.GUI
{
    public static class ExtensionMethods
    {
        public static TimeSpan ToTimeSpan(this double value)
        {
            return TimeSpan.FromTicks((long) (value * 10_000_000));
        }
        public static BitmapSource GetScreenShot(this MediaElement source, int zoom)
        {
            var scale = source.NaturalVideoHeight / source.RenderSize.Height;

            var actualHeight = source.RenderSize.Height;
            var actualWidth = source.RenderSize.Width;
            var renderHeight = actualHeight * scale;
            var renderWidth = actualWidth * scale;

            var renderTarget = new RenderTargetBitmap((int) renderWidth,
                (int) renderHeight, 96, 96, PixelFormats.Pbgra32);
            var sourceBrush = new VisualBrush(source);
            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0),
                    new Point(actualWidth, actualHeight)));
            }

            renderTarget.Render(drawingVisual);
            var zoomedBitmap = new TransformedBitmap(renderTarget, new ScaleTransform(zoom, zoom));
            var croppedBitmap = new CroppedBitmap(zoomedBitmap, new Int32Rect(
                (int) (zoomedBitmap.Width / 2 - renderWidth / 2),
                (int) (zoomedBitmap.Height / 2 - renderHeight / 2),
                (int) renderWidth,
                (int) renderHeight
            ));
            return croppedBitmap;
        }
    }
}