using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using ShadowClip.GUI.UploadDialog;

namespace ShadowClip.GUI
{
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