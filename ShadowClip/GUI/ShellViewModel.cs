using System.Windows;
using Caliburn.Micro;

namespace ShadowClip.GUI
{
    public sealed class ShellViewModel : Screen
    {
        private readonly ISettings _settings;


        public ShellViewModel(VideoViewModel videoViewModel,
            FileSelectViewModel fileSelectViewModel,
            StatusViewModel statusViewModel,
            ISettings settings)
        {
            _settings = settings;
            VideoViewModel = videoViewModel;
            FileSelectViewModel = fileSelectViewModel;
            StatusViewModel = statusViewModel;
            DisplayName = "Shadow Clip";
        }

        public double Top
        {
            get { return _settings.Top; }
            set { _settings.Top = value; }
        }

        public double Left
        {
            get { return _settings.Left; }
            set { _settings.Left = value; }
        }

        public double Height
        {
            get { return _settings.Height; }
            set { _settings.Height = value; }
        }

        public double Width
        {
            get { return _settings.Width; }
            set { _settings.Width = value; }
        }

        public WindowState Maximized
        {
            get { return _settings.Maximized ? WindowState.Maximized : WindowState.Normal; }
            set { _settings.Maximized = value == WindowState.Maximized; }
        }

        public GridLength FilePanelWidth
        {
            get { return new GridLength(_settings.FilePanelWidth); }
            set { _settings.FilePanelWidth = value.Value; }
        }

        public VideoViewModel VideoViewModel { get; }
        public FileSelectViewModel FileSelectViewModel { get; }
        public StatusViewModel StatusViewModel { get; }

        public void OnClosing()
        {
            _settings.Save();
        }
    }
}