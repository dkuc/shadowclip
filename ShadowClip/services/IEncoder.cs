using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IEncoder
    {
        Task Encode(string originalFile, string outputFile, IEnumerable<SegmentCollection> segments, bool useGpu, bool forceWideScreen,
            IProgress<EncodeProgress> encodeProgress, CancellationToken cancelToken);

        Task Encode(IReadOnlyList<FileInfo> clips, string outputFile, bool useGpu, bool forceWideScreen,
            Progress<EncodeProgress> encodeProgress, CancellationToken cancelToken);
    }

    public class EncodeProgress
    {
        public EncodeProgress(int percentComplete, int framesPerSecond)
        {
            PercentComplete = percentComplete;
            FramesPerSecond = framesPerSecond;
        }

        public int PercentComplete { get; }
        public int FramesPerSecond { get; }
    }
}