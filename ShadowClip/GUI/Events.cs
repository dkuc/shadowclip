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

    public class RequestFileDelete
    {
        public RequestFileDelete(FileInfo file)
        {
            File = file;
        }

        public FileInfo File { get; }
    }
}