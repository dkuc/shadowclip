using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public class FfmpegEncoder : IEncoder
    {
        private Task Encode(string ffmpegCommand, double duration, IProgress<EncodeProgress> encodeProgresss,
            CancellationToken cancelToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var lastOutput = "";

            try
            {
                if (duration <= 0)
                    throw new Exception("Invalid start and end times.");
                var process = new Process
                {
                    StartInfo =
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = @"ffmpeg.exe",
                        Arguments = ffmpegCommand
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

        public Task Encode(string originalFile, string outputFile, IEnumerable<Segment> segments,
            bool useGpu, IProgress<EncodeProgress> encodeProgresss, CancellationToken cancelToken)
        {
            var duration = segments.Sum(segment => (segment.End - segment.Start) / (double) segment.Speed);

            return Encode(BuildFfmpegCommand(), duration, encodeProgresss, cancelToken);

            string BuildFfmpegCommand()
            {
                var filter = "";
                var index = 0;
                var concat = "";
                foreach (var segment in segments)
                {
                    index++;
                    var hasSpeedTransform = segment.Speed != 1;
                    var suffix = hasSpeedTransform ? "tmp" : "";
                    var zoom = segment.Zoom;
                    var zoomFilter = zoom > 1 ? $",scale={zoom}*iw:-1, crop = iw / {zoom}:ih / {zoom}" : "";
                    filter +=
                        $"[0:v]trim=start={segment.Start}:duration={segment.End - segment.Start}, setpts=PTS-STARTPTS{zoomFilter}[video{index}{suffix}];";
                    filter +=
                        $"[0:a]atrim=start={segment.Start}:duration={segment.End - segment.Start}, asetpts=PTS-STARTPTS[audio{index}{suffix}];";

                    concat += $"[video{index}][audio{index}]";
                    if (hasSpeedTransform)
                    {
                        var extraSlow = segment.Speed > 2 || segment.Speed < 0.5m;
                        var audioSuffix = extraSlow ? "tmp2" : "";
                        var audioSpeed = Math.Max(0.5, Math.Min(2d, (double) segment.Speed));
                        filter += $"[video{index}tmp]setpts=PTS/{segment.Speed}[video{index}];";
                        filter += $"[audio{index}tmp]atempo={audioSpeed}[audio{index}{audioSuffix}];";
                        if (extraSlow)
                            filter += $"[audio{index}tmp2]atempo={audioSpeed}[audio{index}];";
                    }
                }

                concat += $"concat=n={segments.Count()}:v=1:a=1[final]";
                filter += concat;

                var encoder = useGpu ? "h264_nvenc" : "libx264 ";
                var command =
                    $"-nostdin -i \"{originalFile}\" -c:v {encoder} -filter_complex \"{filter}\"  -global_quality:v 33 -movflags faststart -f mp4 -y -map [final] \"{outputFile}\"";
                Console.WriteLine(command);
                return command;
            }
        }

        public async Task Encode(IReadOnlyList<FileInfo> clips, string outputFile, bool useGpu,
            Progress<EncodeProgress> encodeProgress,
            CancellationToken cancelToken)
        {
            var duration = await GetFullDuration(clips);

            await Encode(BuildFfmpegCommand(), duration, encodeProgress, cancelToken);

            string BuildFfmpegCommand()
            {
                var filter = string.Join(" ", Enumerable.Range(0, clips.Count).Select(i => $"[{i}:v] [{i}:a] "));
                filter += $"concat=n={clips.Count}:v=1:a=1[final]";
                var encoder = useGpu ? "h264_nvenc" : "libx264 ";
                var inputFiles = string.Join(" ", clips.Select(file => $"-i \"{file.FullName}\""));
                var command =
                    $"-nostdin {inputFiles} -c:v {encoder} -filter_complex \"{filter}\"  -global_quality:v 33 -movflags faststart -f mp4 -y -map [final] \"{outputFile}\"";
                Console.WriteLine(command);
                return command;
            }
        }

        private async Task<double> GetFullDuration(IReadOnlyList<FileInfo> clips)
        {
            var durations = await Task.WhenAll(clips.Select(clip => GetDuration(clip.FullName)));
            return durations.Sum();
        }

        private async Task<double> GetDuration(string videoFilePath)
        {
            var process = new Process
            {
                StartInfo =
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = @"ffmpeg.exe",
                    Arguments = $"-i \"{videoFilePath}\""
                }
            };

            process.Start();
            var stream = process.StandardError;
            var line = "No Output";
            while (!stream.EndOfStream)
            {
                line = await stream.ReadLineAsync();
                var pattern = @"Duration: (\d\d:\d\d:\d\d\.\d\d)";
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    var timeString = match.Groups[1].Value;
                    return TimeSpan.Parse(timeString).TotalSeconds;
                }
            }

            throw new Exception($"Could not get duration of video: {line}");
        }
    }
}