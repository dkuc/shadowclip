using System.IO;

namespace ShadowClip.GUI
{
    public class FileSelected
    {
        public FileSelected(FileInfo file)
        {
            File = file;
        }

        public FileInfo File { get; }
    }
}