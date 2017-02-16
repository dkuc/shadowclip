using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public class ClipCreator : IClipCreator
    {
        private readonly IEncoder _encoder;
        private readonly IUploader _uploader;

        public ClipCreator(IEncoder encoder, IUploader uploader)
        {
            _encoder = encoder;
            _uploader = uploader;
        }

        public async Task ClipAndUpload(string originalFile, string clipName, int start, int end,
            IProgress<EncodeProgress> encodeProgress, IProgress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            var outputFile = Path.GetTempFileName();
            try
            {
                await _encoder.Encode(originalFile, outputFile, start, end, encodeProgress, cancelToken);
                await _uploader.UploadFile(outputFile, clipName, uploadProgress, cancelToken);
            }
            finally
            {
                try
                {
                    File.Delete(outputFile);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }
        }
    }
}