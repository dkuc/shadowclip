using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using Caliburn.Micro;
using ShadowClip.GUI.UploadDialog;
using ShadowClip.services;
using static System.IO.Path;
using Application = System.Windows.Application;
using Screen = Caliburn.Micro.Screen;

namespace ShadowClip.GUI
{
    public sealed class FileSelectViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettings _settings;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IDialogBuilder _dialogBuilder;
        private readonly IFileDeleter _fileDeleter;
        private readonly BindableCollection<VideoFile> _videos = new BindableCollection<VideoFile>();
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        public FileSelectViewModel(EventAggregator eventAggregator, ISettings settings,
            IThumbnailGenerator thumbnailGenerator, IDialogBuilder dialogBuilder, IFileDeleter fileDeleter)
        {
            _eventAggregator = eventAggregator;
            _settings = settings;
            _thumbnailGenerator = thumbnailGenerator;
            _dialogBuilder = dialogBuilder;
            _fileDeleter = fileDeleter;
            Path = _settings.ShadowplayPath;
            Videos = CollectionViewSource.GetDefaultView(_videos);
            Videos.SortDescriptions.Add(new SortDescription("CreationTime", ListSortDirection.Descending));
            _eventAggregator.Subscribe(this);
        }


        public bool ShowPreviews
        {
            get => _settings.ShowPreviews;
            set => _settings.ShowPreviews = value;
        }

        public bool ShowFileNames
        {
            get => _settings.ShowFileNames;
            set => _settings.ShowFileNames = value;
        }

        public string Path
        {
            get => _settings.ShadowplayPath;
            set => _settings.ShadowplayPath = value;
        }

        public ICollectionView Videos { get; }

        public VideoFile SelectedVideo { get; set; }

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

                var filesOnDisk = allDirectories.SelectMany(dir => dir.GetFiles("*.mp4"));

                var newFiles =
                    filesOnDisk.Where(info => _videos.All(video => video.File.FullName != info.FullName)).ToList();
                var deletedFiles =
                    _videos.Where(video => filesOnDisk.All(file => video.File.FullName != file.FullName)).ToList();
                //Gotta add/delete one by one to stop the entire list from re-rendering
                foreach (var deletedFile in deletedFiles)
                    _videos.Remove(deletedFile);
                foreach (var addedFile in newFiles)
                    _videos.Add(new VideoFile(addedFile, _thumbnailGenerator));

                _watchers.AddRange(allDirectories.Select(info => new FileSystemWatcher(info.FullName)));

                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = true;
                    watcher.Created += OnFileWatchEvent;
                    watcher.Renamed += OnFileWatchEvent;
                    watcher.Deleted += OnFileWatchEvent;
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

        public void OnSelectedVideoChanged()
        {
            _eventAggregator.PublishOnCurrentThread(new FileSelected(SelectedVideo?.File));
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

        public void Rename(VideoFile video)
        {
            var renameDialog = new RenameDialog
            {
                ResponseText = video.Name.Trim(".mp4".ToCharArray()),
                Owner = Application.Current.MainWindow
            };

            var dialogResult = renameDialog.ShowDialog();
            if (dialogResult != null && dialogResult.Value)
            {
                if (video == SelectedVideo)
                    SelectedVideo = null;

                var directory = GetDirectoryName(video.File.FullName);

                if (directory == null)
                    return;

                var destFileName = Combine(directory, renameDialog.ResponseText + ".mp4");
                _videos.Remove(video);
                video.File.MoveTo(destFileName);
            }
        }

        public async void DeleteSingle(VideoFile video)
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete this video?",
                    "Delete Files",
                    MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                    await DeleteAndUnselect(video);
            }
            catch (Exception e)
            {
                MessageBox.Show("It didn't work: " + e.Message);
            }
        }
        public async void Delete()
        {
            try
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete the selected videos?",
                    "Delete Files",
                    MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                    foreach (var selectedFile in _videos.Where(video => video.IsSelected).ToList())
                        await DeleteAndUnselect(selectedFile);
            }
            catch (Exception e)
            {
                MessageBox.Show("It didn't work: " + e.Message);
            }
        }

        public void CombineClips()
        {
            var videoFiles = _videos.Where(video => video.IsSelected);
            if (videoFiles.Count() < 2)
            {
                MessageBox.Show("You must select at least two clips.");
                return;
            }
            _dialogBuilder.BuildDialog<UploadClipViewModel>(new UploadData(videoFiles));
        }

        private async Task DeleteAndUnselect(VideoFile video)
        {
            if (video == SelectedVideo)
                SelectedVideo = null;
            await _fileDeleter.Delete(video.File);
            _videos.Remove(video);
        }
    }
}