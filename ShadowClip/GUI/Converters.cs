using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ShadowClip.GUI.UploadDialog;

namespace ShadowClip.GUI
{
    internal class VideoZoomConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(values[0] is int naturalWidth)) return null;
            if (!(values[1] is int naturalHeight)) return null;
            if (!(values[2] is double width)) return null;
            if (!(values[3] is double height)) return null;
            if (!(parameter is string dimension)) return null;

            var isHeight = dimension == "height";

            var aspectRatio = naturalWidth / (double) naturalHeight;
            if (height * aspectRatio > width)
                return isHeight ? width / aspectRatio / 2 : width / 2;

            return isHeight ? height / 2 : height * aspectRatio / 2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class VideoSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(values[0] is string source)) return null;
            if (!(values[1] is bool showVideo)) return null;

            return showVideo ? new Uri(source) : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class DurationMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = new Thickness();
            if (!(values[0] is TimeSpan start)) return thickness;
            if (!(values[1] is TimeSpan end)) return thickness;
            if (!(values[2] is TimeSpan duration)) return thickness;
            if (!(values[3] is double width)) return thickness;
            if (duration.Ticks == 0) return thickness;

            var startOffset = start.TotalSeconds / duration.TotalSeconds * width;
            var endOffset = end.TotalSeconds / duration.TotalSeconds * width;

            return new Thickness(startOffset, 0, width - endOffset, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateToBoolConverter : ParamConverter<State>
    {
        public override object Convert(State state, string parameter)
        {
            var strings = parameter.Split(',');

            return strings.Any(s =>
            {
                Enum.TryParse(s, true, out State value);
                return value == state;
            });
        }
    }

    internal class BitRateConverter : SimpleConverter<int>
    {
        public override object Convert(int bytesPerSecond)
        {
            var ordinals = new[] {"", "K", "M", "G", "T", "P", "E"};

            var rate = (decimal) bytesPerSecond;

            var ordinal = 0;

            while (rate > 1024)
            {
                rate /= 1024;
                ordinal++;
            }
            var roundedValue = Math.Round(rate, 2, MidpointRounding.AwayFromZero);

            return $"{roundedValue} {ordinals[ordinal]}B/s";
        }
    }

    internal class MaxLengthConverter : SimpleConverter<TimeSpan>
    {
        public override object Convert(TimeSpan duration)
        {
            return duration.TotalSeconds;
        }
    }

    internal class IntegerSecondsConverter : SimpleConverter<TimeSpan>
    {
        public override object Convert(TimeSpan time)
        {
            return time.ToString("m\\:ss\\.ff");
        }
    }

    internal class PlayActionCoverter : SimpleConverter<MediaState>
    {
        public override object Convert(MediaState mediaState)
        {
            if (mediaState == MediaState.Play)
                return "Pause";

            return "Play";
        }
    }

    internal abstract class SimpleConverter<T> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert((T) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public abstract object Convert(T value);
    }

    internal abstract class ParamConverter<T> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert((T) value, (string) parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public abstract object Convert(T value, string parameter);
    }
}