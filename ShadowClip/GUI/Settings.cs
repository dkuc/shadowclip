namespace ShadowClip.GUI
{
    internal class Settings : ISettings
    {
        private readonly Properties.Settings _settings = new Properties.Settings();

        public bool ShowFileNames
        {
            get { return _settings.ShowFileNames; }
            set { _settings.ShowFileNames = value; }
        }

        public bool ShowPreviews
        {
            get { return _settings.ShowPreviews; }
            set { _settings.ShowPreviews = value; }
        }

        public string ShadowplayPath
        {
            get { return _settings.ShadowplayPath; }
            set { _settings.ShadowplayPath = value; }
        }

        public bool IsMuted
        {
            get { return _settings.IsMuted; }
            set { _settings.IsMuted = value; }
        }

        public void Save()
        {
            _settings.Save();
        }
    }

    public interface ISettings
    {
        bool IsMuted { get; set; }
        string ShadowplayPath { get; set; }
        bool ShowFileNames { get; set; }
        bool ShowPreviews { get; set; }
        void Save();
    }
}