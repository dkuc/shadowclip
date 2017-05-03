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
        public UploadData(FileInfo originalFile, double startTime, double endTime, int zoom, int slowMo)
        {
            OriginalFile = originalFile;
            StartTime = startTime;
            EndTime = endTime;
            Zoom = zoom;
            SlowMo = slowMo;
        }

        public FileInfo OriginalFile { get; }
        public double StartTime { get; }
        public double EndTime { get; }
        public int Zoom { get; }
        public int SlowMo { get; }
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
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettings _settings;
        private CancellationTokenSource _cancelToken;

        public UploadClipViewModel(IClipCreator clipCreator, ISettings settings, IEventAggregator eventAggregator,
            UploadData data)
        {
            _clipCreator = clipCreator;
            _settings = settings;
            _eventAggregator = eventAggregator;
            OriginalFile = data.OriginalFile;
            StartTime = data.StartTime;
            EndTime = data.EndTime;
            Zoom = data.Zoom;
            SlowMo = data.SlowMo;
            FileName = "";
            DisplayName = "Uploader";
        }

        public int SlowMo { get; set; }

        public int Zoom { get; }

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
        public bool UseFfmpeg { get; set; }

        public string ErrorText { get; set; }
        public string YouTubeId { get; set; } = "";

        public string Url => BuildUrl(SelectedDestination, SafeFileName);

        public Destination SelectedDestination { get; set; }
        public Array Destinations => Enum.GetValues(typeof(Destination));

        private string BuildUrl(Destination destination, string fileName)
        {
            switch (destination)
            {
                case Destination.DanSite:
                    return $"https://dankuc.com/videos/{Uri.EscapeUriString(fileName)}";
                case Destination.File:
                    return $"{Path.Combine(_settings.ShadowplayPath, fileName)}.mp4";
                default:
                    return $"https://www.youtube.com/watch?v={YouTubeId}";
            }
        }


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
                YouTubeId =  await _clipCreator.ClipAndUpload(OriginalFile.FullName, $"{SafeFileName}.mp4",
                    StartTime,
                    EndTime,
                    Zoom,
                    SlowMo,
                    UseFfmpeg,
                    SelectedDestination,
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
                    _eventAggregator.PublishOnCurrentThread(new RequestFileDelete(OriginalFile));
            }
            catch (Exception e)
            {
                CurrentState = State.Error;
                Console.Write(e);
                ErrorText = $"Error: {e.Message}";
            }
            finally
            {
                _cancelToken = null;
            }
        }


        public void Cancel()
        {
            _cancelToken?.Cancel();
        }

        public void OnUrlClick(MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    Process.Start(Url);
            }
            catch
            {
                // ignored
            }
        }

        public void Copy()
        {
            Clipboard.SetText(Url);
        }

        protected override void OnDeactivate(bool close)
        {
            Cancel();
        }

        protected override void OnActivate()
        {
            UseFfmpeg = _settings.UseFfmpeg;
            SelectedDestination = _settings.Destination;
        }

        public void OnUseFfmpegChanged()
        {
            _settings.UseFfmpeg = UseFfmpeg;
        }
        public void OnSelectedDestinationChanged()
        {
            _settings.Destination = SelectedDestination;
        }
    }
}