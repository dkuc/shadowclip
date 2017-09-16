using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IClipCreator
    {
        Task<string> ClipAndUpload(string originalFileFullName, string clipName, IEnumerable<Segment> segments,
            bool useFfmpeg, Destination selectedDestination, Progress<EncodeProgress> useGpu,
            Progress<UploadProgress> destination, CancellationToken cancelTokenToken);
    }
}