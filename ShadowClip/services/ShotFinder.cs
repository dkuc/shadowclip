using System;
using System.Windows.Controls;
using System.IO;
using System.Collections.Generic;
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

namespace ShadowClip.services
{
    public interface IShotFinder
    {
        Task<IEnumerable<double>> GetShotTimes(string videoFilePath);
    }

    public class ShotFinder : IShotFinder
    {
        private readonly IModelService _modelService;
        private readonly IAudioService _audioService;
        private readonly IFingerprintCommandBuilder _fingerprintCommandBuilder ;
        private readonly IQueryCommandBuilder _queryCommandBuilder;

        public ShotFinder(InMemoryModelService modelService,
                          NAudioService audioService,
                          FingerprintCommandBuilder fingerprintCommandBuilder,
                          QueryCommandBuilder queryCommandBuilder)
        {
            _modelService = modelService;
            _audioService = audioService;
            _fingerprintCommandBuilder = fingerprintCommandBuilder;
            _queryCommandBuilder = queryCommandBuilder;
        }

        public void StoreAudioFileFingerprintsInStorageForLaterRetrieval(string pathToAudioFile,
                                                                         string title,
                                                                         int length)
        {
            var track = new TrackData("1", title, "test-song", "test-album", 2017, length);

            // store track metadata in the datasource
            var trackReference = _modelService.InsertTrack(track);

            // create hashed fingerprints
            var hashedFingerprints = _fingerprintCommandBuilder
                                     .BuildFingerprintCommand()
                                     .From(pathToAudioFile)
                                     .WithFingerprintConfig(
                                     config =>
                                     {
                                         config.Stride = new SoundFingerprinting.Strides.IncrementalRandomStride(1, 1); //256 512
                                     })
                                     .UsingServices(_audioService)
                                     .Hash()
                                     .Result;

            // store hashes in the database for later retrieval
            _modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);
        }

        public QueryResult GetBestMatchForSong(string queryAudioFile, int secondsToAnalyze, int startTime)
        {
            var queryResult = _queryCommandBuilder.BuildQueryCommand()
                              .From(queryAudioFile, secondsToAnalyze, startTime)
                              .WithFingerprintConfig(
                              config =>
                              {
                                  config.Stride = new SoundFingerprinting.Strides.IncrementalRandomStride(1, 1); //256 512
                              })
                              .UsingServices(_modelService, _audioService)
                              .Query()
                              .Result;

            return queryResult;
            // return queryResult.BestMatch.Track; // successful match has been found
        }

        private QueryResult ProcessAudio(string videoFilePath)
        {
            //audio location
            //This has a hard time picking out awp shots in noisy environments
            try
            {
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
                            $"-nostdin -i \"{videoFilePath}\" -vn -ar 44100 -ac 2 -ab 192k -f mp3 \"temp.mp3\""
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.Write(e);
            }

            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/awp_01.wav", "awp_01", 4);
            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/awp_02.wav", "awp_02", 4);
            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/awp1.wav", "awp1", 4);
            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/zoom.wav", "zoom", 1);
            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/zoom.wav", "awp_boltback", 1);
            StoreAudioFileFingerprintsInStorageForLaterRetrieval("C:/Users/Chris/Downloads/awp_sounds/awp/zoom.wav", "awp_boltforward", 1);
            QueryResult result = GetBestMatchForSong("temp.mp3", 10, 109); //lenght, start
            return result;
        }

        private Boolean HasFlash(Mat image, Double width, Double height)
        {
            int halfWidth = Convert.ToInt32(width / 2);
            int halfHeight = Convert.ToInt32(height / 2);
            int quarterWidth = halfWidth / 2;
            int quarterHeight = halfHeight / 2;
            Mat croppedMat = new Mat(image, new OpenCvSharp.Rect(halfWidth, halfHeight, quarterWidth, quarterHeight));

            Mat orangeMat = new Mat();
            Mat yellowMat = new Mat();
            Mat combinedMat = new Mat();
            Cv2.InRange(croppedMat, new Scalar(75, 123, 195), new Scalar(97, 145, 217), orangeMat); //orange
            Cv2.InRange(croppedMat, new Scalar(94, 200, 245), new Scalar(108, 245, 255), yellowMat); //yellow
            Cv2.AddWeighted(orangeMat, 1.0, yellowMat, 1.0, 1.0, combinedMat);
            Cv2.Threshold(combinedMat, combinedMat, 16, 255, ThresholdTypes.Binary);

            double totalPixelsFlash = quarterWidth * quarterHeight;
            double whitePixelsFlash = Convert.ToDouble(combinedMat.CountNonZero());

            return (whitePixelsFlash > (totalPixelsFlash * 0.001));
        }

        private IEnumerable<double> ProcessVideo(string videoFilePath)
        {
            //process video file in opencv
            videoFilePath = videoFilePath.Replace("%20", " ");
            VideoCapture capture = new VideoCapture(videoFilePath);
            List<double> shotTimes = new List<double>();

            using (Mat image = new Mat())
            {
                var previousFrameIsScope = false;
                var currentFrameIsScope = false;
                Boolean checkSecondFrame = false;

                while (true)
                {
                    previousFrameIsScope = currentFrameIsScope;

                    capture.Read(image);
                    if (image.Empty())
                            break;
                    Mat grayMat = new Mat();
                    Cv2.CvtColor(image, grayMat, ColorConversionCodes.BGR2GRAY);

                    Cv2.Threshold(grayMat, grayMat, 16, 255, ThresholdTypes.Binary);
                    Mat dst = new Mat();
                    Cv2.GaussianBlur(grayMat, dst, new OpenCvSharp.Size(5, 5), 0.5, 0.5);

                    double width = System.Convert.ToDouble(capture.FrameWidth);
                    double height = System.Convert.ToDouble(capture.FrameHeight);
                    double totalPixels = width * height;
                    double whitePixels = System.Convert.ToDouble(grayMat.CountNonZero());
                    double blackPixels = totalPixels - whitePixels;
                    double minPercent = 0.45;
                    double maxPercent = 0.9;
                    double minBlackPixels = totalPixels * minPercent;
                    double maxBlackPixels = totalPixels * maxPercent;

                    if (blackPixels > minBlackPixels && blackPixels < maxBlackPixels)
                    {
                        currentFrameIsScope = true;
                    } else
                    {
                        currentFrameIsScope = false;
                    }

                    if ((currentFrameIsScope == false && previousFrameIsScope == true) || checkSecondFrame == true)
                    {
                        //check for a muzzle flash for 2 frames after the descope
                        if (HasFlash(image, width, height))
                        {
                            var offset = checkSecondFrame == true ? 2 : 1;
                            var time = (capture.PosFrames - offset) / capture.Fps;
                            shotTimes.Add(time);
                            checkSecondFrame = false;
                            /* for debugging
                            using (new Window("capture", combinedMat))
                            {
                                Cv2.WaitKey();
                            }
                            */
                        }
                        else
                        {
                            checkSecondFrame = checkSecondFrame == true ? false : true;
                        }


                    }
                } 
            }

            return shotTimes.ToArray();

/*
            //text recognition, for kill detection
            var engine = new TesseractEngine(@"C:/Users/chris/Downloads/tessdata", "eng", EngineMode.Default);
            var img = Pix.LoadFromFile("temp.png");
            var page = engine.Process(img);
            var text = page.GetText();
            var wordString = "";
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
                                wordString = wordString + word + " ";

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

*/
        }

        public async Task<IEnumerable<double>> GetShotTimes(string videoFilePath)
        {
            //await Task.Run(() => ProcessAudio(videoFilePath));
            return await Task.Run(() => ProcessVideo(videoFilePath));
        }

    }
}
