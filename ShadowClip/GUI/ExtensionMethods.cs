using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Unosquare.FFME;

namespace ShadowClip.GUI
{
    public static class ExtensionMethods
    {
        public static TimeSpan ToTimeSpan(this double value)
        {
            return TimeSpan.FromTicks((long)(value * 10_000_000));
        }

        public static async Task<BitmapSource> GetScreenShot(this MediaElement source, int zoom)
        {
            var result = (await source.CaptureBitmapAsync()).CreateBitmapSourceFromBitmap();

            var renderHeight = (double)source.NaturalVideoHeight;
            var renderWidth = (double)source.NaturalVideoWidth;

            var zoomedBitmap = new TransformedBitmap(result, new ScaleTransform(zoom, zoom));
            var croppedBitmap = new CroppedBitmap(zoomedBitmap, new Int32Rect(
                (int)(zoomedBitmap.Width / 2 - renderWidth / 2),
                (int)(zoomedBitmap.Height / 2 - renderHeight / 2),
                (int)renderWidth,
                (int)renderHeight
            ));
            return croppedBitmap;
        }

        public static BitmapSource CreateBitmapSourceFromBitmap(this Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            return Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
    }
}