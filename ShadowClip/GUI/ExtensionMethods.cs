using System;
using System.IO;
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

        public static BitmapSource GetScreenShot(this MediaElement source)
        {
            var scale = source.NaturalVideoHeight / source.RenderSize.Height;

            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;
            double renderHeight = actualHeight * scale;
            double renderWidth = actualWidth * scale;

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
            return renderTarget;
        }
    }
}