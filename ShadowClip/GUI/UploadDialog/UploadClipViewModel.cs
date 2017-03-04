using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using ShadowClip.services;

namespace ShadowClip.GUI.UploadDialog
{
    public class UploadData
    {
        public UploadData(FileInfo originalFile, double startTime, double endTime)
        {
            OriginalFile = originalFile;
            StartTime = startTime;
            EndTime = endTime;
        }

        public FileInfo OriginalFile { get; }
        public double StartTime { get; }
        public double EndTime { get; }
    }

    public enum State
    {
        Ready,
        Working,
        Error,
        Done
    }

    public sealed class UploadClipViewModel : Screen
    {
        private readonly IClipCreator _clipCreator;
        private CancellationTokenSource _cancelToken;

        public UploadClipViewModel(IClipCreator clipCreator, UploadData data)
        {
            _clipCreator = clipCreator;
            OriginalFile = data.OriginalFile;
            StartTime = data.StartTime;
            EndTime = data.EndTime;
            FileName = "";
            DisplayName = "Uploader";
        }

        public State CurrentState { get; set; }

        public FileInfo OriginalFile { get; }
        public string FileName { get; set; }

        public string SafeFileName => string.Join("_",
            FileName.Split(Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(new[] {' '})
                .ToArray()));

        public double StartTime { get; }
        public double EndTime { get; }

        public bool CurrentlyUploading { get; set; }
        public bool CurrentlyEncoding { get; set; }

        public int EncodeProgress { get; set; }
        public int EncodeFps { get; set; }
        public int UploadProgress { get; set; }
        public int UploadRate { get; set; }
        public bool OperationInProgress { get; set; }
        public bool DeleteOnSuccess { get; set; }

        public string ErrorText { get; set; }

        public string Url => $"https://dankuc.com/videos/{Uri.EscapeUriString(SafeFileName)}";


        public async void StartUpload()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                MessageBox.Show("Please enter a file name");
                return;
            }


            try
            {
                CurrentState = State.Working;
                ErrorText = "";
                _cancelToken = new CancellationTokenSource();
                await _clipCreator.ClipAndUpload(OriginalFile.FullName, $"{SafeFileName}.mp4",
                    StartTime,
                    EndTime,
                    new Progress<EncodeProgress>(ep =>
                    {
                        EncodeProgress = ep.PercentComplete;
                        EncodeFps = ep.FramesPerSecond;
                    }),
                    new Progress<UploadProgress>(up =>
                    {
                        UploadProgress = up.PercentComplete;
                        UploadRate = up.BitsPerSecond;
                    }), _cancelToken.Token);
                CurrentState = State.Done;
                if (DeleteOnSuccess)
                    try
                    {
                        File.Delete(OriginalFile.FullName);
                    }
                    catch
                    {
                        // ignored
                    }
            }
            catch (Exception e)
            {
                CurrentState = State.Error;
                Console.Write(e);
                ErrorText = $"Error: {e.Message}";
            }
        }

        public void Cancel()
        {
            _cancelToken?.Cancel();
        }

        public void OnUrlClick(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Process.Start(Url);
        }

        public void Copy()
        {
            Clipboard.SetText(Url);
        }

        protected override void OnDeactivate(bool close)
        {
            Cancel();
        }
    }
}