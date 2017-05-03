using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace ShadowClip.services
{
    public class YouTubeUploader : IUploader
    {
        public Task<string> UploadFile(string filePath, string name, IProgress<UploadProgress> uploadProgress,
            CancellationToken cancelToken)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            Run().ContinueWith(Done);

            return taskCompletionSource.Task;

            async Task Run()
            {
                var clientSecrets = new ClientSecrets
                {
                    ClientId = "",
                    ClientSecret = ""
                };
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    new[] {YouTubeService.Scope.YoutubeUpload},
                    "user",
                    cancelToken
                );

                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential
                });

                var video = new Video
                {
                    Snippet = new VideoSnippet
                    {
                        Title = name,
                        Description = name,
                        CategoryId = "22"
                    },
                    Status = new VideoStatus {PrivacyStatus = "unlisted"}
                };

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var fileSize = fileStream.Length;
                    var videosInsertRequest =
                        youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    videosInsertRequest.ProgressChanged += VideosInsertRequestProgressChanged;
                    videosInsertRequest.ResponseReceived += VideosInsertRequestResponseReceived;

                    await videosInsertRequest.UploadAsync(cancelToken);

                    void VideosInsertRequestProgressChanged(IUploadProgress progress)
                    {
                        switch (progress.Status)
                        {
                            case UploadStatus.Uploading:
                                UpdateProgress(progress);
                                break;

                            case UploadStatus.Failed:
                                taskCompletionSource.TrySetException(progress.Exception);
                                break;
                        }
                    }

                    void UpdateProgress(IUploadProgress progress)
                    {
                        var speed = progress.BytesSent / stopwatch.ElapsedMilliseconds * 1000;
                        var percentComplete = progress.BytesSent / (double) fileSize * 100;
                        uploadProgress.Report(new UploadProgress((int) percentComplete, (int) speed));
                    }


                    void VideosInsertRequestResponseReceived(Video completedVideo)
                    {
                        var speed = fileSize / stopwatch.ElapsedMilliseconds * 1000;
                        uploadProgress.Report(new UploadProgress(100, (int) speed));
                        taskCompletionSource.SetResult(completedVideo.Id);
                    }
                }
            }


            void Done(Task task)
            {
                if (task.IsFaulted && task.Exception != null)
                    taskCompletionSource.TrySetException(task.Exception);

                if (cancelToken.IsCancellationRequested)
                    taskCompletionSource.TrySetException(new Exception("Upload Canceled"));
            }
        }
    }
}