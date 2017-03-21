using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public class FfmpegEncoder : IEncoder
    {
        public Task Encode(string originalFile, string outputFile, double start, double end, int zoom, int slowMo,
            IProgress<EncodeProgress> encodeProgresss, CancellationToken cancelToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var lastOutput = "";

            start = start * slowMo;
            end = end * slowMo;

            var duration = end - start;

            try
            {
                if (duration <= 0)
                    throw new Exception("Invalid start and end times.");


                var slowMotionFilter = "";
                var audioFilter = slowMo == 2 ? "-filter:a \"atempo=0.5\"" : "";
                if (slowMo > 1)
                {
                    slowMotionFilter = $"setpts={slowMo}*PTS";
                }


                var zoomFilter = zoom > 1 ? $"scale={zoom}*iw:-1, crop = iw / {zoom}:ih / {zoom}" : "";


                var delim = zoom > 1 && slowMo > 1 ? "," : "";

                var videoFilter = $"-vf \"{zoomFilter} {delim} {slowMotionFilter}\"";

                if (zoom == 1 && slowMo == 1)
                {
                    videoFilter = "";
                }


                var process = new Process
                {
                    StartInfo =
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = @"ffmpeg.exe",
                        Arguments =
                            $"-nostdin -i \"{originalFile}\" -c:v h264_nvenc -ss {start} -t {duration} {videoFilter} {audioFilter}  -global_quality:v 29 -movflags faststart -f mp4 -y \"{outputFile}\""
                    }
                };

                cancelToken.Register(() =>
                {
                    try
                    {
                        process.Kill();
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                });
                process.Exited += (sender, eventArgs) =>
                {
                    if (process.ExitCode == 0)
                    {
                        taskCompletionSource.TrySetResult(true);
                        return;
                    }

                    var message = cancelToken.IsCancellationRequested
                        ? "Encode canceled"
                        : "Encoding Failed: " + lastOutput;
                    taskCompletionSource.TrySetException(new Exception(message));
                };
                process.EnableRaisingEvents = true;
                process.Start();
                EmitProgress(process.StandardError);
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }


            return taskCompletionSource.Task;

            async void EmitProgress(StreamReader stream)
            {
                var buffer = new char[65536];
                var length = 1;
                while (length > 0)
                {
                    length = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var output = new string(buffer, 0, length);
                    var lines = output.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        lastOutput = line;
                        var pattern = @"fps=(.*) q=.*time=(.*) bitrate";
                        var match = Regex.Match(line, pattern);
                        if (match.Success)
                        {
                            var timeString = match.Groups[2].Value;
                            var fpsString = match.Groups[1].Value;
                            var fps = double.Parse(fpsString);
                            var timeSpan = TimeSpan.Parse(timeString);
                            encodeProgresss.Report(new EncodeProgress((int) (timeSpan.TotalSeconds / duration * 100),
                                (int) fps));
                        }
                    }
                }
            }
        }
    }
}