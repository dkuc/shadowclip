namespace ShadowClip.GUI
{
    internal class Settings : ISettings
    {
        private readonly Properties.Settings _settings = new Properties.Settings();

        public bool IsMuted
        {
            get { return _settings.IsMuted; }
            set
            {
                _settings.IsMuted = value;
                _settings.Save();
            }
        }

        public string ShadowplayPath
        {
            get { return _settings.ShadowplayPath; }
            set
            {
                _settings.ShadowplayPath = value;
                _settings.Save();
            }
        }

        public string HandbrakePath
        {
            get { return _settings.HandbrakePath; }
            set
            {
                _settings.HandbrakePath = value;
                _settings.Save();
            }
        }
    }

    public interface ISettings
    {
        bool IsMuted { get; set; }
        string ShadowplayPath { get; set; }
        string HandbrakePath { get; set; }
    }
}