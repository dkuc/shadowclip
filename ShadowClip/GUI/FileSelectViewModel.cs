using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Forms;
using Caliburn.Micro;
using Screen = Caliburn.Micro.Screen;

namespace ShadowClip.GUI
{
    public sealed class FileSelectViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly BindableCollection<FileInfo> _files = new BindableCollection<FileInfo>();
        private readonly ISettings _settings;
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher();

        public FileSelectViewModel(EventAggregator eventAggregator, ISettings settings)
        {
            _eventAggregator = eventAggregator;
            _settings = settings;
            PropertyChanged += PropertyUpdated;
            Path = _settings.ShadowplayPath;
            Files = CollectionViewSource.GetDefaultView(_files);
            Files.SortDescriptions.Add(new SortDescription("CreationTime", ListSortDirection.Descending));
        }


        public bool ShowPreviews
        {
            get { return _settings.ShowPreviews; }
            set { _settings.ShowPreviews = value; }
        }

        public bool ShowFileNames
        {
            get { return _settings.ShowFileNames; }
            set { _settings.ShowFileNames = value; }
        }

        public string Path
        {
            get { return _settings.ShadowplayPath; }
            set { _settings.ShadowplayPath = value; }
        }

        public ICollectionView Files { get; }

        public FileInfo SelectedFile { get; set; }

        public string ErrorMessage { get; set; }

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
                var filesOnDisk = new DirectoryInfo(Path).GetFiles();
                var newFiles =
                    filesOnDisk.Where(info => _files.All(fileInfo => fileInfo.FullName != info.FullName)).ToList();
                var deletedFiles =
                    _files.Where(info => filesOnDisk.All(fileInfo => info.FullName != fileInfo.FullName)).ToList();
                //Gotta add/delete one by one to stop the entire list from re-rendering
                foreach (var deletedFile in deletedFiles)
                    _files.Remove(deletedFile);
                foreach (var addedFile in newFiles)
                    _files.Add(addedFile);

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

        public void Delete(FileInfo file)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this?", "Delete File",
                    MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    file.Delete();
                    _files.Remove(file);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("It didn't work: " + e.Message);
            }
        }
    }
}