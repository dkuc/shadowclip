using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IClipCreator
    {
        Task ClipAndUpload(string originalFile, string clipName, double start, double end,
            int zoom,
            bool useFfmpeg,
            IProgress<EncodeProgress> encodeProgress,
            IProgress<UploadProgress> uploadProgress, CancellationToken cancelToken);
    }
}