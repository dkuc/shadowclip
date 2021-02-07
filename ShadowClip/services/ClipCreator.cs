using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using ShadowClip.GUI;

namespace ShadowClip.services
{
    public enum Destination
    {
        Shadowclip,
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

        public Task<string> ClipAndUpload(string originalFile, string clipName, IEnumerable<SegmentCollection> timelines,
            Encoder encoder, bool forceWideScreen, Destination destination, Progress<EncodeProgress> encodeProgress,
            Progress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            Task EncodeAction(string outputFile) => _encoder.Encode(originalFile, outputFile, timelines, encoder, forceWideScreen, encodeProgress, cancelToken);
            return ClipAndUpload(EncodeAction, clipName, destination, uploadProgress, cancelToken);
        }

        public Task<string> ClipAndUpload(IReadOnlyList<FileInfo> clips, string clipName, Encoder encoder, bool forceWideScreen,
            Destination destination, Progress<EncodeProgress> encodeProgress,
            Progress<UploadProgress> uploadProgress, CancellationToken cancelToken)
        {
            Task EncodeAction(string outputFile) => _encoder.Encode(clips, outputFile, encoder, forceWideScreen, encodeProgress, cancelToken);
            return ClipAndUpload(EncodeAction, clipName, destination, uploadProgress, cancelToken);
        }


        private async Task<string> ClipAndUpload(Func<string, Task> encodeAction, string clipName,
            Destination destination, Progress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            var outputFile = Path.GetTempFileName();
            try
            {
                await encodeAction(outputFile);
                if (destination == Destination.File)
                {
                    var destFileName = Path.Combine(_settings.ShadowplayPath, clipName);
                    File.Move(outputFile, destFileName);
                    return "";
                }

                var uploader = destination == Destination.Shadowclip
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