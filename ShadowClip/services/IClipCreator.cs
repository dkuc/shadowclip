using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IClipCreator
    {
        Task<string> ClipAndUpload(string originalFileFullName, string clipName, IEnumerable<Segment> segments,
            bool useGpu, Destination destination, Progress<EncodeProgress> encodeProgress,
            Progress<UploadProgress> uploadProgress, CancellationToken cancelToken);

        Task<string> ClipAndUpload(IReadOnlyList<FileInfo> clips, string clipName,
            bool useGpu, Destination destination, Progress<EncodeProgress> encodeProgress,
            Progress<UploadProgress> uploadProgress, CancellationToken cancelToken);
    }
}