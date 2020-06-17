using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IThumbnailGenerator
    {
        Task Generate(FileInfo file);
    }

    public class ThumbnailGenerator : IThumbnailGenerator
    {
        private readonly string _thumbnailPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "shadowclip",
                "thumbnails");

        private Task _previousCall;

        public async Task Generate(FileInfo file)
        {
            Directory.CreateDirectory(_thumbnailPath);

            _previousCall = _previousCall == null ? CreateThumbnail(file) : CreateThumbnailDeferred(file);

            await _previousCall;
        }

        private async Task CreateThumbnailDeferred(FileInfo file)
        {
            await _previousCall;
            await CreateThumbnail(file);
        }

        private Task CreateThumbnail(FileInfo missingThumbmail)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            var ffmpegCommand =
                $"-ss 0 -i  \"{missingThumbmail.FullName}\" -vframes 1 -q:v 2 -filter:v scale=\"133:-1\" \"{Path.Combine(_thumbnailPath, missingThumbmail.Name + ".jpg")}\"";
            var process = new Process
            {
                StartInfo =
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = @"ffmpeg_binaries/ffmpeg.exe",
                    Arguments = ffmpegCommand
                }
            };

            process.Exited += (sender, eventArgs) =>
            {
                if (process.ExitCode != 0)
                    Console.WriteLine("Thumbnail generation failed.");

                taskCompletionSource.TrySetResult(true);
            };
            process.EnableRaisingEvents = true;
            process.Start();

            return taskCompletionSource.Task;
        }
    }
}