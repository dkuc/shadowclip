using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;

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