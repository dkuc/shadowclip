using Caliburn.Micro;

namespace ShadowClip.GUI
{
    public class ShellViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;

        public ShellViewModel(VideoViewModel videoViewModel,
            FileSelectViewModel fileSelectViewModel,
            StatusViewModel statusViewModel,
            IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
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
            _eventAggregator.PublishOnUIThread(new WindowClosing());
        }
    }
}