using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using ShadowClip.GUI.Controls;
using ShadowClip.GUI.UploadDialog;
using ShadowClip.services;

namespace ShadowClip.GUI
{
    public sealed class VideoViewModel : Screen, IHandle<FileSelected>
    {
        private const long MinUpdateTime = 32;
        private readonly IDialogBuilder _dialogBuilder;
        private readonly TimeSpan _frameTime = TimeSpan.FromTicks(166667);
        private readonly ISettings _settings;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private FileInfo _currentFile;
        private TimeSpan _newestPosition;
        private bool _playingWhileClicked;
        private DispatcherTimer _previewTimer;
        private long _previousTimeSet;
        private Task _setTask;
        private VideoView _videoView;

        public VideoViewModel(IEventAggregator eventAggregator, ISettings settings, IDialogBuilder dialogBuilder)
        {
            _settings = settings;
            _dialogBuilder = dialogBuilder;
            eventAggregator.Subscribe(this);

            Segments.CollectionChanged += SegmentsOnCollectionChanged;
            Segments.Add(new Segment {Start = 0, End = 60, Speed = 1, Zoom = 1});
            _stopwatch.Start();
        }

        public BindableCollection<Segment> Segments { get; set; } = new BindableCollection<Segment>();

        public int Zoom
        {
            get => CurrentSegment.Zoom;
            set => CurrentSegment.Zoom = value;
        }

        public decimal Speed
        {
            get => CurrentSegment.Speed;
            set => CurrentSegment.Speed = value;
        }

        public bool IsMuted { get; set; }

        public MediaElement VideoPlayer => _videoView.Video;

        public MediaState CurrentMediaState => VideoPlayer.GetMediaState();

        public TimeSpan Position => VideoPlayer.Position;

        public Segment FirstSegment => Segments.First();
        public Segment FinalSegment => Segments.Last();

        public Segment CurrentSegment
        {
            get
            {
                if (Segments.Count == 1)
                    return FirstSegment;

                if (CurrentPosition < FirstSegment.End)
                    return FirstSegment;

                foreach (var segment in Segments)
                    if (CurrentPosition >= segment.Start && CurrentPosition < segment.End)
                        return segment;

                return FinalSegment;
            }
        }

        public TimeSpan Duration => VideoPlayer.NaturalDuration.HasTimeSpan
            ? VideoPlayer.NaturalDuration.TimeSpan
            : TimeSpan.Zero;

        public TimeSpan StartPosition
        {
            get => FirstSegment.Start.ToTimeSpan();
            set => FirstSegment.Start = value.TotalSeconds;
        }

        public TimeSpan EndPosition
        {
            get => FinalSegment.End.ToTimeSpan();
            set => FirstSegment.End = value.TotalSeconds;
        }

        public double CurrentPosition
        {
            get => Position.TotalSeconds;

            set => SetPostion(value.ToTimeSpan());
        }

        public void Handle(FileSelected message)
        {
            _currentFile = message.File;
            if (message.File == null)
            {
                VideoPlayer.Source = null;
                return;
            }

            VideoPlayer.Source = new Uri(message.File.FullName);
            VideoPlayer.Play();
            SetPostion(TimeSpan.Zero);
        }

        private void SegmentsOnCollectionChanged(object o, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems != null)
                foreach (INotifyPropertyChanged newItem in args.NewItems)
                    newItem.PropertyChanged += SegmentChanged;
            if (args.OldItems != null)
                foreach (INotifyPropertyChanged oldItem in args.OldItems)
                    oldItem.PropertyChanged -= SegmentChanged;
        }

        public void AddSegment()
        {
            var newSegStart = CurrentSegment.Start;
            var newSegEnd = CurrentPosition;
            if (CurrentPosition < FirstSegment.Start)
            {
                newSegEnd = CurrentSegment.Start;
            }

            else if (CurrentPosition > FinalSegment.End)
            {
                newSegStart = FinalSegment.Start;
                newSegEnd = FinalSegment.End;
                FinalSegment.Start = FinalSegment.End;
            }
            else
            {
                CurrentSegment.Start = CurrentPosition;
            }

            var newSegment = new Segment {Start = newSegStart, End = newSegEnd, Speed = 1, Zoom = 1};
            
            Segments.Insert(Segments.IndexOf(CurrentSegment), newSegment);
        }

        public void RemoveSegment()
        {
            if (Segments.Count > 1)
                Segments.RemoveAt(Segments.Count - 1);
        }

        private void SegmentChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var segment = (Segment) o;
            var propertyName = propertyChangedEventArgs.PropertyName;
            if (propertyName == "Start")
                SetPostion(segment.Start.ToTimeSpan());
            if (propertyName == "End" && segment == FinalSegment)
                SetPostion(segment.End.ToTimeSpan());
        }

        public void OnIsMutedChanged()
        {
            _settings.IsMuted = IsMuted;
        }

        public void OnSpeedChanged()
        {
            VideoPlayer.SpeedRatio = (double) Speed;
        }

        public void MarkStart()
        {
            StartPosition = Position;
        }

        public void MarkEnd()
        {
            EndPosition = Position;
        }

        private async void SetPostion(TimeSpan position)
        {
            _newestPosition = position;
            if (_setTask != null)
                return;
            var elapsedTime = _stopwatch.ElapsedMilliseconds - _previousTimeSet;
            if (elapsedTime < MinUpdateTime)
            {
                _setTask = Task.Delay((int) (MinUpdateTime - elapsedTime));
                await _setTask;
                _setTask = null;
            }

            VideoPlayer.Pause();
            VideoPlayer.Position = _newestPosition;
            NotifyOfPropertyChange(() => CurrentMediaState);
            NotifyOfPropertyChange(() => CurrentPosition);
            NotifyOfPropertyChange(() => Zoom);
            NotifyOfPropertyChange(() => Speed);
            _previousTimeSet = _stopwatch.ElapsedMilliseconds;
        }

        protected override void OnViewAttached(object view, object context)
        {
            _videoView = (VideoView) view;
            VideoPlayer.MediaOpened += VideoPlayerOnMediaOpened;

            IsMuted = _settings.IsMuted;

            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(.5)};
            timer.Tick += (sender, args) =>
            {
                NotifyOfPropertyChange("");
                if (Speed != (decimal) VideoPlayer.SpeedRatio) OnSpeedChanged();
            };
            timer.Start();
        }


        private void VideoPlayerOnMediaOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            Segments.RemoveRange(Segments.Except(new[] {FirstSegment}).ToList());
            StartPosition = TimeSpan.Zero;
            var duration = VideoPlayer.NaturalDuration;
            EndPosition = duration.HasTimeSpan ? duration.TimeSpan : TimeSpan.Zero;
            CurrentPosition = 0;
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
                _dialogBuilder.BuildDialog<UploadClipViewModel>(new UploadData(_currentFile, Segments));
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

        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void VideoClicked(MouseButtonEventArgs eventArgs)
        {
            _videoView.VideoSlider.Focus();
            var firstPostition = eventArgs.GetPosition(_videoView).X;
            var previousPosition = firstPostition;
            var lastUpdate = DateTime.Now;
            MouseEventHandler videoViewOnMouseMove = (sender, args) =>
            {
                if ((DateTime.Now - lastUpdate).TotalMilliseconds < 10)
                    return;

                VideoPlayer.Pause();
                var newPosition = args.GetPosition(_videoView).X;
                var delta = newPosition - previousPosition;
                previousPosition = newPosition;
                var videoPlayerPosition = TimeSpan.FromTicks((long) (delta * 10000));
                VideoPlayer.Position += videoPlayerPosition;

                lastUpdate = DateTime.Now;
            };

            MouseButtonEventHandler videoViewOnMouseUp = null;
            MouseEventHandler onMouseLostFocus = null;
            videoViewOnMouseUp = (sender, args) =>
            {
                Mouse.Capture(null);
                _videoView.MouseMove -= videoViewOnMouseMove;
                _videoView.MouseUp -= videoViewOnMouseUp;
                // ReSharper disable once AssignNullToNotNullAttribute
                Mouse.RemoveLostMouseCaptureHandler(_videoView, onMouseLostFocus);
                if (Math.Abs(previousPosition - firstPostition) < .001)
                    TogglePlay();
            };

            onMouseLostFocus = (sender, args) =>
            {
                Mouse.Capture(null);
                _videoView.MouseMove -= videoViewOnMouseMove;
                Mouse.RemoveLostMouseCaptureHandler(_videoView, onMouseLostFocus);
            };


            Mouse.AddLostMouseCaptureHandler(_videoView, onMouseLostFocus);
            _videoView.MouseUp += videoViewOnMouseUp;
            _videoView.MouseMove += videoViewOnMouseMove;
            Mouse.Capture(_videoView);
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
            var screenShot = VideoPlayer.GetScreenShot(Zoom);

            Clipboard.SetImage(screenShot);
        }

        public void PreviewClicked(SegClicked clickEvent)
        {
            var segment = clickEvent.Segment;
            var start = segment.Start;
            var end = segment.End;
            SetPostion(start.ToTimeSpan());
            OnSpeedChanged();
            VideoPlayer.Play();

            _previewTimer?.Stop();
            _previewTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(16)};

            _previewTimer.Tick += (sender, args) =>
            {
                if (CurrentMediaState != MediaState.Play)
                    _previewTimer.Stop();
                if (VideoPlayer.Position >= end.ToTimeSpan())
                {
                    VideoPlayer.Pause();
                    _previewTimer.Stop();
                }
            };
            _previewTimer.Start();
        }
    }
}