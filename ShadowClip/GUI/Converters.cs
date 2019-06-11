using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ShadowClip.GUI.UploadDialog;
using ShadowClip.services;

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

    internal class ListPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var item = values[0];
            var list = (IList) values[1];
            return $"{list.IndexOf(item) + 1}.";
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

    internal class NameStateTextConverter : SimpleConverter<NameState>
    {
        public override object Convert(NameState state)
        {
            switch (state)
            {
                case NameState.Empty:
                    return "";
                case NameState.Working:
                    return "Checking availability...";
                case NameState.Taken:
                    return "File Name Taken";
                case NameState.Available:
                    return "Available";
                case NameState.Overwritable:
                    return "File Exists - Overwritable";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }

    internal class NameStateBackgroundConverter : SimpleConverter<NameState>
    {
        public override object Convert(NameState state)
        {
            switch (state)
            {
                case NameState.Taken:
                    return new SolidColorBrush(Colors.IndianRed);
                case NameState.Available:
                    return new SolidColorBrush(Colors.MediumSeaGreen);
                case NameState.Overwritable:
                    return new SolidColorBrush(Colors.Yellow);
                default:
                    return DependencyProperty.UnsetValue;
            }
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

    internal class SpeedConverter : SimpleConverter<decimal>
    {
        public override object Convert(decimal speed)
        {
            if (speed > 1)
                return new SolidColorBrush(Colors.LightSalmon);

            if (speed < 1)
                return new SolidColorBrush(Colors.LightBlue);

            return new SolidColorBrush(Colors.LightGreen);
        }
    }

    internal class MaxLengthConverter : SimpleConverter<TimeSpan>
    {
        public override object Convert(TimeSpan duration)
        {
            return duration.TotalSeconds;
        }
    }

    internal class DestinationToVisibilityConverter : SimpleConverter<Destination>
    {
        public override object Convert(Destination duration)
        {
            return duration == Destination.File ? Visibility.Collapsed : Visibility.Visible;
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

    internal class BooleanToThicknessConverter : SimpleConverter<bool>
    {
        public override object Convert(bool isTrue)
        {
            return new Thickness(isTrue ? 2 : 0);
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

    internal class InverseBooleanToVisibilityConverter : SimpleConverter<bool>
    {
        public override object Convert(bool isHidden)
        {
            return isHidden ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}