using System;
using System.IO;
using System.Threading.Tasks;
using PropertyChanged;
using ShadowClip.services;

namespace ShadowClip.GUI
{
    [AddINotifyPropertyChangedInterface]
    public class VideoFile
    {
        private readonly IThumbnailGenerator _generator;
        private readonly string _thumbnailPath;
        private Task _generateTask;
        private string _thumbnail;

        public VideoFile(FileInfo file, IThumbnailGenerator generator)
        {
            _generator = generator;
            File = file;
            _thumbnailPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "shadowclip",
                    "thumbnails", file.Name + ".jpg");
        }

        public FileInfo File { get; }
        public string Name => File.Name;
        public DateTime CreationTime => File.CreationTime;
        public bool IsSelected { get; set; }


        public string Thumbnail
        {
            get
            {
                if (_generateTask != null)
                    return _thumbnail;

                if (_thumbnail != null)
                    return _thumbnail;

                if (System.IO.File.Exists(_thumbnailPath))
                    _thumbnail = _thumbnailPath;
                else
                    _generateTask = GenerateThumnail();

                return _thumbnail;
            }
            set => _thumbnail = value;
        }

        private async Task GenerateThumnail()
        {
            await _generator.Generate(File);
            Thumbnail = _thumbnailPath;
        }
    }
}