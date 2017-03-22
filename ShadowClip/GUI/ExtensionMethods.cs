using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShadowClip.GUI
{
    public static class ExtensionMethods
    {
        //Nasty Hack because microsoft only allows the media state to be observed in Windows 10 Universal apps, not WPF
        public static MediaState GetMediaState(this MediaElement mediaElement)
        {
            var hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            var helperObject = hlp.GetValue(mediaElement);
            var stateField = helperObject.GetType()
                .GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            var state = (MediaState) stateField.GetValue(helperObject);
            return state;
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
                (int) (zoomedBitmap.Width / 2 -  renderWidth / 2),
                (int) (zoomedBitmap.Height / 2 -  renderHeight / 2),
                (int) renderWidth,
                (int) renderHeight
                ));
            return croppedBitmap;
        }
    }
}