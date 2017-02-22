using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using ShadowClip.GUI.UploadDialog;

namespace ShadowClip.GUI
{
    public sealed class VideoViewModel : Screen, IHandle<FileSelected>, IHandle<WindowClosing>
    {
        private readonly IDialogBuilder _dialogBuilder;
        private readonly TimeSpan _frameTime = TimeSpan.FromMilliseconds(1000 / (double) 60);
        private readonly ISettings _settings;
        private FileInfo _currentFile;
        private bool _playingWhileClicked;
        private bool _supressEvents;
        private VideoView _videoView;

        public VideoViewModel(IEventAggregator eventAggregator, ISettings settings, IDialogBuilder dialogBuilder)
        {
            _settings = settings;
            _dialogBuilder = dialogBuilder;
            PropertyChanged += PropertyUpdated;
            eventAggregator.Subscribe(this);
        }

        public MediaElement VideoPlayer => _videoView.Video;

        public MediaState CurrentMediaState => VideoPlayer.GetMediaState();

        public TimeSpan Position => VideoPlayer.Position;


        public TimeSpan Duration
            => VideoPlayer.NaturalDuration.HasTimeSpan ? VideoPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;

        public int StartPostion { get; set; }

        public int EndPostion { get; set; }

        public double CurrentPosition
        {
            get { return Position.TotalSeconds; }

            set
            {
                VideoPlayer.Pause();
                VideoPlayer.Position = TimeSpan.FromSeconds(value);
                NotifyOfPropertyChange(() => Position);
            }
        }

        public void Handle(FileSelected message)
        {
            _currentFile = message.File;
            VideoPlayer.Source = new Uri(message.File.FullName);
            VideoPlayer.Play();
            SetPostion(0);
        }

        public void Handle(WindowClosing message)
        {
            _settings.IsMuted = VideoPlayer.IsMuted;
        }

        private void PropertyUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (_supressEvents)
                return;

            if (e.PropertyName == "StartPostion")
                SetPostion(StartPostion);
            if (e.PropertyName == "EndPostion")
                SetPostion(EndPostion);
        }

        private void SetPostion(int position)
        {
            SetPostion(TimeSpan.FromSeconds(position));
        }

        private void SetPostion(TimeSpan position)
        {
            VideoPlayer.Pause();
            VideoPlayer.Position = position;
            NotifyOfPropertyChange(() => CurrentMediaState);
            NotifyOfPropertyChange(() => CurrentPosition);
        }

        protected override void OnViewAttached(object view, object context)
        {
            _videoView = (VideoView) view;
            VideoPlayer.MediaOpened += VideoPlayerOnMediaOpened;
            VideoPlayer.IsMuted = _settings.IsMuted;
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            timer.Tick += (sender, args) => NotifyOfPropertyChange("");
            timer.Start();
        }

        private void VideoPlayerOnMediaOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            _supressEvents = true;
            EndPostion = (int) VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            _supressEvents = false;
        }

        public void TogglePlay()
        {
            if (CurrentMediaState == MediaState.Play)
                VideoPlayer.Pause();
            else
                VideoPlayer.Play();
            NotifyOfPropertyChange(() => CurrentMediaState);
        }

        public void Upload()
        {
            if (_currentFile != null)
                _dialogBuilder.BuildDialog<UploadClipViewModel>(new UploadData(_currentFile, StartPostion, EndPostion));
        }

        public void GoToNextFrame()
        {
            SetPostion(Position.Add(_frameTime));
        }

        public void GoToPreviousFrame()
        {
            SetPostion(Position.Subtract(_frameTime));
        }

        public void KeyPressed(KeyEventArgs keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Key.Right:
                    GoToNextFrame();
                    keyEvent.Handled = true;
                    break;
                case Key.Left:
                    GoToPreviousFrame();
                    keyEvent.Handled = true;
                    break;
                case Key.Space:
                    TogglePlay();
                    keyEvent.Handled = true;
                    break;
            }
        }

        public void VideoClicked()
        {
            TogglePlay();
            _videoView.VideoSlider.Focus();
        }

        public void SliderClicked()
        {
            _playingWhileClicked = CurrentMediaState == MediaState.Play;
        }

        public void SliderReleased()
        {
            if (_playingWhileClicked)
                VideoPlayer.Play();
        }

        public void Screenshot()
        {
            var screenShot = VideoPlayer.GetScreenShot();

            Clipboard.SetImage(screenShot);
        }
    }
}