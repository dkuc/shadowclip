using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IClipCreator
    {
        Task ClipAndUpload(string originalFile, string clipName, int start, int end,
            IProgress<EncodeProgress> encodeProgress,
            IProgress<UploadProgress> uploadProgress, CancellationToken cancelToken);
    }
}