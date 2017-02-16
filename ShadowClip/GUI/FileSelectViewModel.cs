using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Caliburn.Micro;
using Screen = Caliburn.Micro.Screen;

namespace ShadowClip.GUI
{
    public sealed class FileSelectViewModel : Screen, IHandle<WindowClosing>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettings _settings;
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher();

        public FileSelectViewModel(EventAggregator eventAggregator, ISettings settings)
        {
            _eventAggregator = eventAggregator;
            _settings = settings;
            PropertyChanged += PropertyUpdated;
            Path = _settings.ShadowplayPath;
            eventAggregator.Subscribe(this);
        }

        public IEnumerable<FileInfo> Files { get; set; }

        public FileInfo SelectedFile { get; set; }

        public string ErrorMessage { get; set; }

        public string Path { get; set; }

        public void Handle(WindowClosing message)
        {
            _settings.ShadowplayPath = Path;
        }

        protected override void OnViewLoaded(object view)
        {
            if (string.IsNullOrEmpty(Path))
                ErrorMessage = "Click Browse to open a directory";
            else
                LoadPath();

            _watcher.Created += (sender, args) => LoadPath();
            _watcher.Renamed += (sender, args) => LoadPath();
            _watcher.Deleted += (sender, args) => LoadPath();
        }

        private void LoadPath()
        {
            ErrorMessage = "";
            try
            {
                Files = new DirectoryInfo(Path).GetFiles().OrderByDescending(info => info.CreationTime);
                _watcher.Path = Path;
                _watcher.EnableRaisingEvents = true;
            }
            catch
            {
                ErrorMessage = "Cannot open path";
            }
        }

        private void PropertyUpdated(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "SelectedFile")
                if (SelectedFile != null)
                    _eventAggregator.PublishOnCurrentThread(new FileSelected(SelectedFile));
        }

        public void Browse()
        {
            var folderBrowserDialog = new FolderBrowserDialog {SelectedPath = Path};
            var dialogResult = folderBrowserDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                Path = folderBrowserDialog.SelectedPath;
                LoadPath();
            }
        }
    }
}