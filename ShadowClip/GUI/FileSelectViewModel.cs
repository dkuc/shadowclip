using System;
using System.Collections.Generic;
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
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

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
        }

        private void LoadPath()
        {
            ClearWatchers();
            ErrorMessage = "";
            try
            {
                var topLevelDirectory = new DirectoryInfo(Path);

                var subDirectories = topLevelDirectory.GetDirectories();

                var allDirectories = subDirectories.Concat(new[] {topLevelDirectory});

                var filesOnDisk = allDirectories.SelectMany(dir => dir.GetFiles());

                var newFiles =
                    filesOnDisk.Where(info => _files.All(fileInfo => fileInfo.FullName != info.FullName)).ToList();
                var deletedFiles =
                    _files.Where(info => filesOnDisk.All(fileInfo => info.FullName != fileInfo.FullName)).ToList();
                //Gotta add/delete one by one to stop the entire list from re-rendering
                foreach (var deletedFile in deletedFiles)
                    _files.Remove(deletedFile);
                foreach (var addedFile in newFiles)
                    _files.Add(addedFile);

                _watchers.AddRange(allDirectories.Select(info => new FileSystemWatcher(info.FullName)));

                foreach (var watcher in _watchers)
                {
                    watcher.Created += OnFileWatchEvent;
                    watcher.Renamed += OnFileWatchEvent;
                    watcher.Deleted += OnFileWatchEvent;
                    watcher.EnableRaisingEvents = true;
                }
            }
            catch
            {
                ErrorMessage = "Cannot open path";
            }
        }

        private void OnFileWatchEvent(object sender, FileSystemEventArgs args)
        {
            LoadPath();
        }

        private void ClearWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Created -= OnFileWatchEvent;
                watcher.Renamed -= OnFileWatchEvent;
                watcher.Deleted -= OnFileWatchEvent;
                watcher.EnableRaisingEvents = false;
            }
            _watchers.Clear();
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