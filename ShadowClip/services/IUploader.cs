using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IUploader
    {
        Task<string> UploadFile(string filePath, string name, IProgress<UploadProgress> uploadProgress,
            CancellationToken cancelToken);
    }

    public class UploadProgress
    {
        public UploadProgress(int percentComplete, int bitsPerSecond)
        {
            PercentComplete = percentComplete;
            BitsPerSecond = bitsPerSecond;
        }

        public int PercentComplete { get; }
        public int BitsPerSecond { get; }
    }
}