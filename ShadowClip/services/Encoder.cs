using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HandBrake.ApplicationServices.Interop;
using HandBrake.ApplicationServices.Interop.EventArgs;
using HandBrake.ApplicationServices.Interop.Json.Encode;
using Newtonsoft.Json;

namespace ShadowClip.services
{
    public class Encoder : IEncoder
    {
        public Task Encode(string originalFile, string outputFile, int start, int end,
            IProgress<EncodeProgress> encodeProgresss,
            CancellationToken cancelToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            var instance = new HandBrakeInstance();
            instance.Initialize(1);

            instance.ScanCompleted += (o, args) =>
            {
                try
                {
                    if (start >= end)
                        throw new Exception("Invalid start and end times.");
                    var sourceTitle = instance.Titles.TitleList.FirstOrDefault();
                    if (sourceTitle == null)
                        throw new Exception("Could not read input video.");

                    var settings = GetDefaultSettings();
                    settings.Destination.File = outputFile;
                    dynamic resolution = settings.Filters.FilterList.First(filter => filter.ID == 11).Settings;

                    resolution.width = sourceTitle.Geometry.Width;
                    resolution.height = sourceTitle.Geometry.Height;
                    settings.Source.Range.Start = start * 90000;
                    settings.Source.Range.End = end * 90000;

                    settings.Source.Path = originalFile;

                    cancelToken.Register(() => instance.StopEncode());

                    instance.StartEncode(settings);
                }
                catch (Exception e)
                {
                    taskCompletionSource.TrySetException(e);
                }
            };

            instance.EncodeProgress += (o, args) => NewMeth(args, encodeProgresss);

            instance.EncodeCompleted += (o, args) =>
            {
                try
                {
                    if (args.Error)
                    {
                        if (cancelToken.IsCancellationRequested)
                            throw new Exception("Encoding was canceled");
                        throw new Exception("Encoding failed. I don't know why 'cause this API kinda sucks");
                    }
                    taskCompletionSource.TrySetResult(true);
                }
                catch (Exception e)
                {
                    taskCompletionSource.TrySetException(e);
                }
            };

            instance.StartScan(originalFile, 1, TimeSpan.Zero, 1);

            return taskCompletionSource.Task;
        }

        private void NewMeth(EncodeProgressEventArgs args, IProgress<EncodeProgress> encodeProgresss)
        {
            encodeProgresss.Report(new EncodeProgress((int) (args.FractionComplete * 100), (int) args.AverageFrameRate));
        }

        private JsonEncodeObject GetDefaultSettings()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.First(name => name.Contains("handbrake_settings.json"));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
                // ReSharper disable once AssignNullToNotNullAttribute
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<JsonEncodeObject>(result);
            }
        }
    }
}