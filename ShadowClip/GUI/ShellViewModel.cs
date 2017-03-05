using Caliburn.Micro;

namespace ShadowClip.GUI
{
    public class ShellViewModel : Screen
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
        }

        public VideoViewModel VideoViewModel { get; }
        public FileSelectViewModel FileSelectViewModel { get; }
        public StatusViewModel StatusViewModel { get; }

        public override string DisplayName
        {
            get { return "Shadow Clip"; }
            set { }
        }

        public void OnClosing()
        {
            _settings.Save();
        }
    }
}