using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace ShadowClip.services
{
    public class GifCreator
    {
        public void CreateGif(BitmapSource frame1, BitmapSource frame2)
        {
            var path = Path.Combine(Path.GetTempPath(), "shadowclip");
            var filePath = Path.Combine(path, "shadowclip.gif");
            Directory.CreateDirectory(path);

            using (var collection = new MagickImageCollection())
            {
                collection.AddRange(ConvertToStream(frame1));
                collection[0].AnimationDelay = 50;

                collection.AddRange(ConvertToStream(frame2));
                collection[1].AnimationDelay = 50;

                collection.Optimize();

                collection.Write(filePath);
            }

            Process.Start("explorer.exe", $"/select, \"{filePath}\"");
        }

        private Stream ConvertToStream(BitmapSource bitmap)
        {
            Stream stream = new MemoryStream();

            BitmapEncoder bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));
            bitmapEncoder.Save(stream);
            stream.Position = 0;

            return stream;
        }
    }
}