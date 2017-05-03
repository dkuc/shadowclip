using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using ShadowClip.GUI;

namespace ShadowClip.services
{
    public enum Destination
    {
        DanSite,
        File,
        YouTube
    }

    public class ClipCreator : IClipCreator
    {
        private readonly IUnityContainer _container;
        private readonly IEncoder _encoder;
        private readonly ISettings _settings;


        public ClipCreator(IEncoder encoder, IUnityContainer container, ISettings settings)
        {
            _encoder = encoder;
            _container = container;
            _settings = settings;
        }

        public async Task<string> ClipAndUpload(string originalFile, string clipName, double start, double end,
            int zoom,
            int slowMo, bool useGpu, Destination destination, IProgress<EncodeProgress> encodeProgress,
            IProgress<UploadProgress> uploadProgress, CancellationToken cancelToken)
        {
            var outputFile = Path.GetTempFileName();
            try
            {
                await _encoder.Encode(originalFile, outputFile, start, end, zoom, slowMo, useGpu, encodeProgress,
                    cancelToken);
                if (destination == Destination.File)
                {
                    var destFileName = Path.Combine(_settings.ShadowplayPath, clipName);
                    File.Move(outputFile, destFileName);
                    return "";
                }
                var uploader = destination == Destination.DanSite
                    ? (IUploader) _container.Resolve<FileFormUploader>()
                    : _container.Resolve<YouTubeUploader>();

                return await uploader.UploadFile(outputFile, clipName, uploadProgress, cancelToken);
            }
            finally
            {
                if (destination != Destination.File)
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