using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using ShadowClip.GUI.Controls;
using ShadowClip.GUI.UploadDialog;
using ShadowClip.services;
using Unosquare.FFME.Common;
using MediaElement = Unosquare.FFME.MediaElement;

namespace ShadowClip.GUI
{
    public sealed class VideoViewModel : Screen, IHandle<FileSelected>
    {
        private readonly IDialogBuilder _dialogBuilder;
        private readonly GifCreator _gifCreator;
        private readonly ISettings _settings;
        private FileInfo _currentFile;
        private bool _playingWhileClicked;
        private VideoView _videoView;
        private Segment _previewSegment;

        public VideoViewModel(IEventAggregator eventAggregator, ISettings settings, IDialogBuilder dialogBuilder,
            GifCreator gifCreator, IFileDeleter fileDeleter)
        {
            _settings = settings;
            _dialogBuilder = dialogBuilder;
            _gifCreator = gifCreator;
            eventAggregator.Subscribe(this);
            AddTimeline();
            fileDeleter.OnDelete(OnFileDelete);
        }

        public SegmentCollection Segments => SelectedTimeline ?? Timelines.First();

        public BindableCollection<SegmentCollection> Timelines { get; } = new BindableCollection<SegmentCollection>();
        
        public SegmentCollection SelectedTimeline { get; set; }
        
        public int Zoom
        {
            get => CurrentSegment.Zoom;
            set => CurrentSegment.Zoom = value;
        }

        public decimal Speed
        {
            get => CurrentSegment.Speed;
            set
            {
                SetVideoSpeed(value);
                CurrentSegment.Speed = value;
            }
        }

        public bool IsMuted { get; set; }
        
        public double Volume
        {
            get => _settings.Volume;
            set => _settings.Volume = value;
        }

        public MediaElement VideoPlayer => _videoView.Video;

        public bool IsPlaying => VideoPlayer.IsPlaying;

        public TimeSpan Position
        {
            get => VideoPlayer.Position;
            set => VideoPlayer.Position = value;
        }

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

        public TimeSpan Duration => VideoPlayer.NaturalDuration ?? TimeSpan.Zero;

        public double CurrentPosition
        {
            get => Position.TotalSeconds;

            set => Position = value.ToTimeSpan();
        }

        private async Task OnFileDelete(FileInfo file)
        {
            if (VideoPlayer.Source?.OriginalString == file.FullName)
            {
                await VideoPlayer.Close();
                // The above Close function is buggy and the file is not closed by the
                // time the task completes. This gives it an extra half second to close the file.
                await Task.Delay(500);
            }
        }

        public async void Handle(FileSelected message)
        {
            _currentFile = message.File;
            if (message.File == null)
            {
                await VideoPlayer.Close();
                return;
            }

            await VideoPlayer.Open(new Uri(message.File.FullName));
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

            var newSegment = new Segment(Segments) {Start = newSegStart, End = newSegEnd, Speed = 1, Zoom = 1};
            
            Segments.Insert(Segments.IndexOf(CurrentSegment), newSegment);
        }

        public void RemoveSegment()
        {
            if (Segments.Count > 1)
                Segments.RemoveAt(Segments.Count - 1);
        }
        
        public void AddTimeline()
        {
            if (Timelines.Count < 6)
            {
                var segmentCollection = new SegmentCollection();
                segmentCollection.CollectionChanged += SegmentsOnCollectionChanged;
                segmentCollection.Add(new Segment(segmentCollection) {Start = 0, End = 90, Speed = 1, Zoom = 1});
                Timelines.Add(segmentCollection);
                SelectedTimeline = segmentCollection;
            }
        }

        public void RemoveTimeline()
        {
            if (Timelines.Count > 1)
                Timelines.RemoveAt(Timelines.Count - 1);
        }

        private void SegmentChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var segment = (Segment) o;
            var propertyName = propertyChangedEventArgs.PropertyName;
            if (propertyName == "Start")
                Position = segment.Start.ToTimeSpan();
            if (propertyName == "End" && segment.IsLast)
                Position = segment.End.ToTimeSpan();
        }

        public void OnIsMutedChanged()
        {
            _settings.IsMuted = IsMuted;
        }

        private void SetVideoSpeed(decimal speed)
        {
            VideoPlayer.SpeedRatio = (double) speed;
        }

        protected override void OnViewAttached(object view, object context)
        {
            _videoView = (VideoView) view;
            IsMuted = _settings.IsMuted;

            //ToDo: Change these events to Caliburn messages
            VideoPlayer.MediaOpened += VideoPlayerOnMediaOpened;
            VideoPlayer.MediaStateChanged += VideoPlayerOnMediaStateChanged;
            VideoPlayer.PositionChanged += VideoPlayerOnPositionChanged;
        }

        private void VideoPlayerOnMediaOpened(object sender, MediaOpenedEventArgs mediaOpenedEventArgs)
        {
            if (Segments.Count == 1 && Timelines.Count == 1)
            {
                FirstSegment.Start = 0; // Prevents a crash when End is set to a value smaller than Start
                FirstSegment.End = Duration.TotalSeconds;
                FirstSegment.Start = Duration.TotalSeconds * 0.7;
            }
            CurrentPosition = 0;
        }

        public async void TogglePlay()
        {
            if (IsPlaying)
                await VideoPlayer.Pause();
            else
            {
                SetVideoSpeed(Speed);
                await VideoPlayer.Play();
                
            }

            NotifyOfPropertyChange(() => IsPlaying);
        }

        public void Upload()
        {
            if (_currentFile != null)
                _dialogBuilder.BuildDialog<UploadClipViewModel>(new UploadData(_currentFile, Timelines));
        }

        public async void GoToNextFrame()
        {
            await VideoPlayer.StepForward();
        }

        public async void GoToPreviousFrame()
        {
            await VideoPlayer.StepBackward();
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

            MouseEventHandler videoViewOnMouseMove = (sender, args) =>
            {

                var newPosition = args.GetPosition(_videoView).X;
                var delta = newPosition - previousPosition;
                var videoTimeDelta = TimeSpan.FromTicks((long) (delta * 10000));
                Position = Position + videoTimeDelta;
                previousPosition = newPosition;
                

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
            _playingWhileClicked = IsPlaying;
        }

        public void SliderReleased()
        {
            if (_playingWhileClicked)
                VideoPlayer.Play();
        }

        public async void MakeGif()
        {
            var frame1 = await VideoPlayer.GetScreenShot(Zoom);
            GoToNextFrame();
            await Task.Delay(100);
            var frame2 = await VideoPlayer.GetScreenShot(Zoom);
            _gifCreator.CreateGif(frame1, frame2);

        }

        public async void Screenshot()
        {
            var screenShot = await VideoPlayer.GetScreenShot(Zoom);

            Clipboard.SetImage(screenShot);
        }

        public async void PreviewClicked(SegClicked clickEvent)
        {
            var segment = clickEvent.Segment;
            var start = segment.Start;
            await VideoPlayer.Seek(start.ToTimeSpan());
            SetVideoSpeed(segment.Speed);
            _previewSegment = segment;
            await VideoPlayer.Play();

            _previewSegment = segment;

        }
        private void VideoPlayerOnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            if(_previewSegment != null)
                if (e.Position >= _previewSegment.End.ToTimeSpan())
                    VideoPlayer.Pause();

            if (Speed != (decimal) VideoPlayer.SpeedRatio)
                SetVideoSpeed(Speed);
            NotifyOfPropertyChange("");
        }

        private void VideoPlayerOnMediaStateChanged(object sender, MediaStateChangedEventArgs e)
        {
            NotifyOfPropertyChange("");
            if (e.MediaState != MediaPlaybackState.Play)
            {
                _previewSegment = null;
            }
        }
    }
}