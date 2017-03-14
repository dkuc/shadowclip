using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace ShadowClip.services
{
    public class ClipCreator : IClipCreator
    {
        private readonly IUnityContainer _container;
        private readonly IUploader _uploader;

        public ClipCreator(IUnityContainer container, IUploader uploader)
        {
            _container = container;
            _uploader = uploader;
        }

        public async Task ClipAndUpload(string originalFile, string clipName, double start, double end,
            int zoom, bool useFfmpeg,
            IProgress<EncodeProgress> encodeProgress, IProgress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            var encoder = useFfmpeg
                ? (IEncoder) _container.Resolve<FfmpegEncoder>()
                : _container.Resolve<HandbrakeEncoder>();

            var outputFile = Path.GetTempFileName();
            try
            {
                await encoder.Encode(originalFile, outputFile, start, end, zoom, encodeProgress, cancelToken);
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