using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Caliburn.Micro;
using ShadowClip.services;

namespace ShadowClip.GUI.Controls
{
    public partial class SegmentControl
    {
        private const double SplitWidth = 6;
        private const double FrameTime = 1 / 60.0;

        public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
            "Segments", typeof(BindableCollection<Segment>), typeof(SegmentControl),
            new PropertyMetadata(PropertyChangedCallback));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "DurationTime", typeof(TimeSpan), typeof(SegmentControl), new PropertyMetadata(PropertyChangedCallback));

        private bool _newSegments;
        private bool _newWidths;
        private BindableCollection<Segment> _previousSegments;

        public SegmentControl()
        {
            InitializeComponent();
        }

        public TimeSpan DurationTime
        {
            get => (TimeSpan) GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public double Duration => DurationTime.TotalSeconds;


        public BindableCollection<Segment> Segments
        {
            get => (BindableCollection<Segment>) GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }

        public double UsableWidth => SegGrid.ActualWidth - (Segments.Count + 1) * SplitWidth;
        public event EventHandler Clicked = delegate { };

        private static void PropertyChangedCallback(DependencyObject control, DependencyPropertyChangedEventArgs e)
        {
            if (control is SegmentControl segmentControl)
                segmentControl.Init();
        }

        private void AddSplitter()
        {
            var gridSplitter = new GridSplitter
            {
                Width = SplitWidth,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                KeyboardIncrement = FrameTime
            };
            gridSplitter.DragDelta += OnWidthChange;
            gridSplitter.DragDelta += OnWidthChange;
            SegGrid.Children.Add(gridSplitter);
        }

        private void AddRect(Segment segment)
        {
            var border = new Border
            {
                //Background = new SolidColorBrush(Colors.LightGreen),
                Child = new NoSizeDecorator{Child = new TextBlock {Text = "Preview"}}
            };

            var myBinding = new Binding
            {
                Source = segment,
                Path = new PropertyPath("Speed"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new SpeedConverter()
            };
            BindingOperations.SetBinding(border, Border.BackgroundProperty, myBinding);

            border.PreviewMouseLeftButtonUp += (s, e) => { Clicked(s, new SegClicked(segment)); };
            SegGrid.Children.Add(border);
        }

        private void AddColumn(double value, GridUnitType gridUnitType)
        {
            SegGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(value, gridUnitType)});
        }


        private void SetupElements()
        {
            AddSplitter();
            foreach (var segment in Segments)
            {
                AddRect(segment);
                AddSplitter();
            }

            var column = 0;
            foreach (UIElement child in SegGrid.Children)
            {
                column++;
                Grid.SetColumn(child, column);
            }
        }

        private void SetupColumns()
        {
            AddColumn(1, GridUnitType.Star);
            AddColumn(SplitWidth, GridUnitType.Pixel);
            foreach (var unused in Segments)
            {
                AddColumn(1, GridUnitType.Star);
                AddColumn(SplitWidth, GridUnitType.Pixel);
            }

            AddColumn(1, GridUnitType.Star);
        }


        private void Init()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Duration == 0)
                return;
            if (Segments == null)
                return;

            SegGrid.ColumnDefinitions.Clear();
            SegGrid.Children.Clear();
            SetupColumns();
            SetupElements();
            SetColumnWidths();
            foreach (var segment in Segments)
                // ReSharper disable once SuspiciousTypeConversion.Global
                ((INotifyPropertyChanged) segment).PropertyChanged += SegmentsChanged;
            if (_previousSegments != Segments)
                Segments.CollectionChanged += (sender, args) => Init();

            _previousSegments = Segments;
        }

        private void SegmentsChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_newWidths)
                return;

            _newSegments = true;
            SetColumnWidths();
            _newSegments = false;
        }

        private void SetColumnWidths()
        {
            var width = SegGrid.ActualWidth;

            var usableWidth = width - (Segments.Count + 1) * SplitWidth;
            if (usableWidth < 1)
                return;
            if (Segments.Last().End > Duration)
                return;

            var starColumns = SegGrid.ColumnDefinitions.Where(definition => definition.Width.IsStar).ToList();

            var dividingLines = Segments.Select(segment => segment.Start)
                .Concat(new[] {Segments.Last().End, Duration})
                .ToList();

            var index = 0;
            foreach (var cd in starColumns)
            {
                var previousLine = index == 0 ? 0 : dividingLines[index - 1];
                var dividingLine = dividingLines[index];
                var value = dividingLine - previousLine;
                var pixels = value / Duration * usableWidth;
                cd.Width = new GridLength(pixels, GridUnitType.Star);
                index++;
            }
        }

        private void OnWidthChange(object sender, EventArgs eventArgs)
        {
            if (_newSegments)
                return;

            var width = SegGrid.ActualWidth;
            var usableWidth = width - (Segments.Count + 1) * SplitWidth;
            if (usableWidth < 1)
                return;

            _newWidths = true;
            var starColumns = SegGrid.ColumnDefinitions.Where(definition => definition.Width.IsStar).ToList();
            var index = 0;
            foreach (var segment in Segments)
            {
                var newStart = starColumns.Take(index + 1).Sum(cd => cd.Width.Value) / usableWidth * Duration;
                var newEnd = starColumns.Take(index + 2).Sum(cd => cd.Width.Value) / usableWidth * Duration;
                
                if(Math.Abs(segment.Start - newStart) > 0.001)
                    segment.Start = newStart;

                if (Math.Abs(segment.End - newEnd) > 0.001)
                    segment.End = newEnd;

                index++;
            }

            _newWidths = false;
        }

        private void SegGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Init();
        }
    }

    public class SegClicked : EventArgs
    {
        public SegClicked(Segment segment)
        {
            Segment = segment;
        }

        public Segment Segment { get; }
    }
    public class NoSizeDecorator : Decorator
    {
        protected override Size MeasureOverride(Size constraint) {
            // Ask for no space
            Child.Measure(new Size(0,0));
            return new Size(0, 0);
        }        
    }
}