using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using ShadowClip.GUI.UploadDialog;
using ShadowClip.services;
using System.Threading;
using System.Threading.Tasks;

using OpenCvSharp;
using OpenCvSharp.ML;
using System.Windows.Media.Imaging;

using Tesseract;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Audio;
using SoundFingerprinting;
using SoundFingerprinting.Audio.NAudio;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Query;

namespace ShadowClip.GUI
{
    public sealed class VideoViewModel : Screen, IHandle<FileSelected>
    {
        private readonly IModelService modelService = new InMemoryModelService();
        private readonly IAudioService audioService = new NAudioService();
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder = new FingerprintCommandBuilder();
        private readonly IQueryCommandBuilder queryCommandBuilder = new QueryCommandBuilder();

        private readonly IDialogBuilder _dialogBuilder;
        private readonly IShotFinder _shotFinder;
        private readonly TimeSpan _frameTime = TimeSpan.FromTicks(166667);
        private readonly ISettings _settings;
        private FileInfo _currentFile;
        private bool _playingWhileClicked;
        private VideoView _videoView;

        public VideoViewModel(IEventAggregator eventAggregator, ISettings settings, IDialogBuilder dialogBuilder, IShotFinder shotFinder)

        public void StoreAudioFileFingerprintsInStorageForLaterRetrieval(string pathToAudioFile)
        {
            var track = new TrackData("1", "test-artist", "test-song", "test-album", 2017, 290);

            // store track metadata in the datasource
            var trackReference = modelService.InsertTrack(track);

            // create hashed fingerprints
            var hashedFingerprints = fingerprintCommandBuilder
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash()
                                        .Result;

            // store hashes in the database for later retrieval
            modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);
        }

        public QueryResult GetBestMatchForSong(string queryAudioFile)
        {
            //int secondsToAnalyze = 4; // number of seconds to analyze from query file
            //int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
                                                 //.From(queryAudioFile, secondsToAnalyze, startAtSecond)
            var queryResult = queryCommandBuilder.BuildQueryCommand()
                                                 .From(queryAudioFile, 6, 8)
                                                 .UsingServices(modelService, audioService)
                                                 .Query()
                                                 .Result;

            return queryResult;
            // return queryResult.BestMatch.Track; // successful match has been found
        }

        public VideoViewModel(IEventAggregator eventAggregator, ISettings settings, IDialogBuilder dialogBuilder)
        {
            _settings = settings;
            _dialogBuilder = dialogBuilder;
            _shotFinder = shotFinder;
            eventAggregator.Subscribe(this);
        }

        public int Zoom { get; set; } = 1;

        public int SlowMo { get; set; } = 1;

        public bool IsMuted { get; set; }

        public IEnumerable<double> ShotTimes { get; set; }

        public bool IsFindingShots { get; set; }

        public string IsScopeFrame { get; set; } = "false";

        public string WordString { get; set; } = "";

        public MediaElement VideoPlayer => _videoView.Video;

        public MediaState CurrentMediaState => VideoPlayer.GetMediaState();

        public TimeSpan Position => VideoPlayer.Position;


        public TimeSpan Duration
            => VideoPlayer.NaturalDuration.HasTimeSpan ? VideoPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;

        public TimeSpan StartPosition { get; set; }

        public TimeSpan EndPosition { get; set; }

        public double CurrentPosition
        {
            get { return Position.TotalSeconds; }

            set
            {
                VideoPlayer.Pause();
                VideoPlayer.Position = TimeSpan.FromTicks((long) (value * 10_000_000));
                NotifyOfPropertyChange(() => Position);
            }
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
            ShotTimes = new double[0];
            FindShotTimesAsync();
        }

        private async void FindShotTimesAsync()
        {
            IsFindingShots = true;
            ShotTimes = await _shotFinder.GetShotTimes(VideoPlayer.Source.AbsolutePath);
            IsFindingShots = false;
        }

        public void OnIsMutedChanged()
        {
            _settings.IsMuted = IsMuted;
        }

        public void OnSlowMoChanged()
        {
            VideoPlayer.SpeedRatio = 1 / (double) SlowMo;
        }

        public void MarkStart()
        {
            StartPosition = Position;
        }

        public void MarkEnd()
        {
            EndPosition = Position;
        }

        public void GoTo(ShotTime shotTime)
        {
            CurrentPosition = shotTime.Time;
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

            IsMuted = _settings.IsMuted;

            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            timer.Tick += (sender, args) => NotifyOfPropertyChange("");
            timer.Start();
        }

        private void VideoPlayerOnMediaOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            StartPosition = TimeSpan.Zero;
            var duration = VideoPlayer.NaturalDuration;
            EndPosition = duration.HasTimeSpan ? duration.TimeSpan : TimeSpan.Zero;
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
                _dialogBuilder.BuildDialog<UploadClipViewModel>(new UploadData(_currentFile, StartPosition.TotalSeconds,
                    EndPosition.TotalSeconds, Zoom, SlowMo));
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

            //scope detection
            using (var fileStream = new FileStream("temp.png", FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(screenShot));
                encoder.Save(fileStream);
            }

            Mat train = new Mat("temp.png", ImreadModes.GrayScale);
            Cv2.Threshold(train, train, 16, 255, ThresholdTypes.Binary);
            Mat dst = new Mat();
            Cv2.GaussianBlur(train, dst, new OpenCvSharp.Size(5, 5), 0.5, 0.5);

            double width = System.Convert.ToDouble(VideoPlayer.NaturalVideoWidth);
            double height = System.Convert.ToDouble(VideoPlayer.NaturalVideoHeight);
            double totalPixels = width * height;
            double whitePixels = System.Convert.ToDouble(train.CountNonZero());
            double blackPixels = totalPixels - whitePixels;
            double minPercent = 0.45;
            double maxPercent = 0.9;
            double minBlackPixels = totalPixels * minPercent;
            double maxBlackPixels = totalPixels * maxPercent;

            if (blackPixels > minBlackPixels && blackPixels < maxBlackPixels)
            {
                IsScopeFrame = "true";
            } else
            {
                IsScopeFrame = "false";
            }


            //text recognition
            var engine = new TesseractEngine(@"C:/Users/chris/Downloads/tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile("temp.png");
            var page = engine.Process(img);
            var text = page.GetText();
            using (var iter = page.GetIterator())
            {
                iter.Begin();

                do
                {
                    do
                    {
                        do
                        {
                            do
                            {
                                if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                {
                                    Console.WriteLine("<BLOCK>");
                                }

                                var word = iter.GetText(PageIteratorLevel.Word);
                                WordString = WordString + word + " ";
                                Console.Write(word);
                                Console.Write(" ");

                                if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                {
                                    Console.WriteLine();
                                }
                            } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                            if (iter.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                            {
                                Console.WriteLine();
                            }
                        } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                } while (iter.Next(PageIteratorLevel.Block));
            }

            //audio location
            try
            {
                string videoFile = "C:/Users/Chris/Videos/Counter-strike  Global Offensive/Counter-strike  Global Offensive 04.03.2017 - 22.15.01.26.DVR.mp4";
                var process = new System.Diagnostics.Process
                {
                    StartInfo =
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = @"ffmpeg.exe",
                        Arguments =
                            $"-nostdin -i \"{videoFile}\" -vn -ar 44100 -ac 2 -ab 192k -f mp3 \"temp.mp3\""
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.Write(e);
            }

            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/awp_01.wav");
            QueryResult result = GetBestMatchForSong("temp.mp3"); //2, 11, 20

            using (new OpenCvSharp.Window("Circles", dst))
            {
                Cv2.WaitKey();
            }
        }

        public void PreviewClicked()
        {
            SetPostion(StartPosition);
            VideoPlayer.Play();

            var timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(16)};
            timer.Tick += (sender, args) =>
            {
                if (VideoPlayer.Position >= EndPosition)
                {
                    VideoPlayer.Pause();
                    timer.Stop();
                }
            };
            timer.Start();
        }
    }
}
