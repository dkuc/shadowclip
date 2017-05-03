using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public class FileFormUploader : IUploader
    {
        public async Task<string> UploadFile(string filePath, string name, IProgress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            using (var file = File.OpenRead(filePath))
            {
                var httpClient = new HttpClient();
                var form = new MultipartFormDataContent();

                var progressableStreamContent = new ProgressableStreamContent(file, uploadProgress);
                progressableStreamContent.Headers.Add("Content-Disposition", "form-data");
                progressableStreamContent.Headers.Add("Content-Type", "application/octet-stream");

                progressableStreamContent.Headers.ContentDisposition.Name = "uploadedFile";
                progressableStreamContent.Headers.ContentDisposition.FileName = name;

                form.Add(progressableStreamContent);


                var response = await httpClient.PostAsync("https://dankuc.com/up", form, cancelToken);

                response.EnsureSuccessStatusCode();
                return "";
            }
        }

        internal class ProgressableStreamContent : HttpContent
        {
            private const int BufferSize = 81920;
            private readonly Stream _content;
            private readonly IProgress<UploadProgress> _uploadProgress;


            public ProgressableStreamContent(Stream content, IProgress<UploadProgress> uploadProgress)
            {
                _content = content;
                _uploadProgress = uploadProgress;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var buffer = new byte[BufferSize];
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                while (true)
                {
                    var length = await _content.ReadAsync(buffer, 0, buffer.Length);
                    if (length <= 0) break;
                    await stream.WriteAsync(buffer, 0, length);

                    var percentComplete = (int) (_content.Position / (double) _content.Length * 100);
                    var bytesPerMilisecond = _content.Position / (double) stopwatch.ElapsedMilliseconds;
                    var bytesPerSecond = (int) (bytesPerMilisecond * 1000);
                    _uploadProgress.Report(new UploadProgress(percentComplete, bytesPerSecond));
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _content.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}