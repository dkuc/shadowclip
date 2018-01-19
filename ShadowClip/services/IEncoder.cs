using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IEncoder
    {
        Task Encode(string originalFile, string outputFile, IEnumerable<Segment> segments, bool useGpu,
            IProgress<EncodeProgress> encodeProgress, CancellationToken cancelToken);
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