using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using ShadowClip.services;

namespace ShadowClip.GUI.UploadDialog
{
    public class UploadData
    {
        public IEnumerable<VideoFile> VideoFiles { get; }

        public UploadData(FileInfo originalFile, BindableCollection<SegmentCollection> timelines)
        {
            OriginalFile = originalFile;
            Timelines = timelines;
            IsMultiClip = false;
        }
        
        public UploadData(IEnumerable<VideoFile> videoFiles)
        {
            VideoFiles = videoFiles;
            IsMultiClip = true;
        }

        public bool IsMultiClip { get; }

        public BindableCollection<SegmentCollection> Timelines { get; }

        public FileInfo OriginalFile { get; }
    }

    public enum State
    {
        Ready,
        Working,
        Error,
        Done
    }

    public enum NameState
    {
        Empty,
        Working,
        Taken,
        Available,
        Overwritable
    }

    public sealed class UploadClipViewModel : Screen
    {
        private readonly IJsonWebApiClient _apiClient;
        private readonly IFileDeleter _fileDeleter;
        private readonly IClipCreator _clipCreator;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettings _settings;
        private CancellationTokenSource _cancelToken;

        public UploadClipViewModel(IClipCreator clipCreator,
            ISettings settings,
            IEventAggregator eventAggregator,
            IJsonWebApiClient apiClient,
            IFileDeleter fileDeleter,
            UploadData data)
        {
            _clipCreator = clipCreator;
            _settings = settings;
            _eventAggregator = eventAggregator;
            _apiClient = apiClient;
            _fileDeleter = fileDeleter;
            OriginalFile = data.OriginalFile;
            Timelines = data.Timelines;
            IsMultiClip = data.IsMultiClip;
            VideoFiles = data.IsMultiClip ? new BindableCollection<VideoFile>(data.VideoFiles) : null;
            FileName = "";
            DisplayName = "Uploader";
        }

        public BindableCollection<VideoFile> VideoFiles { get; }

        public bool IsMultiClip { get; }

        public BindableCollection<SegmentCollection> Timelines { get; }

        public decimal Speed => Timelines.First().First().Speed;

        public int Zoom => Timelines.First().First().Zoom;

        public State CurrentState { get; set; }

        public NameState CurrentNameState { get; set; }

        public FileInfo OriginalFile { get; }
        public string FileName { get; set; }

        public string SafeFileName => string.Join("_",
            FileName.Split(Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(new[] {' '})
                .ToArray()));

        public double StartTime => Timelines.First().First().Start;
        public double EndTime => Timelines.First().Last().End;

        public bool CurrentlyUploading { get; set; }
        public bool CurrentlyEncoding { get; set; }

        public int EncodeProgress { get; set; }
        public int EncodeFps { get; set; }
        public int UploadProgress { get; set; }
        public int UploadRate { get; set; }
        public bool OperationInProgress { get; set; }
        public bool DeleteOnSuccess { get; set; }
        public bool UseFfmpeg { get; set; }
        public bool ForceWideScreen { get; set; }

        public string ErrorText { get; set; }
        public string YouTubeId { get; set; } = "";

        public string Url => BuildUrl(SelectedDestination, SafeFileName);

        public Destination SelectedDestination { get; set; }
        public Array Destinations => Enum.GetValues(typeof(Destination));

        public async void OnSafeFileNameChanged()
        {
            await SetNameState();
        }

        private async Task SetNameState()
        {
            if (string.IsNullOrEmpty(SafeFileName))
            {
                CurrentNameState = NameState.Empty;
                return;
            }

            if (SelectedDestination != Destination.Shadowclip)
            {
                CurrentNameState = NameState.Empty;
                return;
            }

            try
            {
                CurrentNameState = NameState.Working;
                var result =
                    await _apiClient.Get($"https://shadowclip.net/info/{Uri.EscapeUriString(SafeFileName)}.mp4");

                if (result.exists == true && result.canDelete == true)
                    CurrentNameState = NameState.Overwritable;
                else if (result.exists == true)
                    CurrentNameState = NameState.Taken;
                else
                    CurrentNameState = NameState.Available;
            }
            catch
            {
                CurrentNameState = NameState.Empty;
            }
        }

        private string BuildUrl(Destination destination, string fileName)
        {
            switch (destination)
            {
                case Destination.Shadowclip:
                    return $"https://shadowclip.net/videos/{Uri.EscapeUriString(fileName)}";
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
                var uploadProgress = new Progress<UploadProgress>(up =>
                {
                    UploadProgress = up.PercentComplete;
                    UploadRate = up.BitsPerSecond;
                });
                var encodeProgress = new Progress<EncodeProgress>(ep =>
                {
                    EncodeProgress = ep.PercentComplete;
                    EncodeFps = ep.FramesPerSecond;
                });

                if (IsMultiClip)
                    YouTubeId = await _clipCreator.ClipAndUpload(VideoFiles.Select(vf => vf.File).ToList(),
                        $"{SafeFileName}.mp4",
                        UseFfmpeg,
                        ForceWideScreen,
                        SelectedDestination,
                        encodeProgress,
                        uploadProgress, _cancelToken.Token);
                else
                    YouTubeId = await _clipCreator.ClipAndUpload(OriginalFile.FullName, $"{SafeFileName}.mp4",
                        Timelines,
                        UseFfmpeg,
                        ForceWideScreen,
                        SelectedDestination,
                        encodeProgress,
                        uploadProgress, _cancelToken.Token);


                CurrentState = State.Done;
                if (!DeleteOnSuccess) return;
                if (!IsMultiClip)
                    await _fileDeleter.Delete(OriginalFile);
                else
                    foreach (var video in VideoFiles)
                        await _fileDeleter.Delete(video.File);
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
            ForceWideScreen = _settings.ForceWideScreen;
            SelectedDestination = _settings.Destination;
        }

        public void OnUseFfmpegChanged()
        {
            _settings.UseFfmpeg = UseFfmpeg;
        }

        public void OnForceWideScreenChanged()
        {
            _settings.ForceWideScreen = ForceWideScreen;
        }

        public void OnSelectedDestinationChanged()
        {
            _settings.Destination = SelectedDestination;
        }

        public void MoveUp(VideoFile video)
        {
            var oldIndex = VideoFiles.IndexOf(video);
            if (oldIndex > 0)
                VideoFiles.Move(oldIndex, oldIndex - 1);
            NotifyOfPropertyChange(() => VideoFiles);
        }

        public void MoveDown(VideoFile video)
        {
            var oldIndex = VideoFiles.IndexOf(video);
            if (oldIndex < VideoFiles.Count - 1)
                VideoFiles.Move(oldIndex, oldIndex + 1);
            NotifyOfPropertyChange(() => VideoFiles);
        }
    }
}