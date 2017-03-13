namespace ShadowClip.GUI
{
    internal class Settings : ISettings
    {
        private readonly Properties.Settings _settings = new Properties.Settings();

        public bool Maximized
        {
            get { return _settings.Maximized; }
            set { _settings.Maximized = value; }
        }

        public double Width
        {
            get { return _settings.Width; }
            set { _settings.Width = value; }
        }

        public double Height
        {
            get { return _settings.Height; }
            set { _settings.Height = value; }
        }

        public double Left
        {
            get { return _settings.Left; }
            set { _settings.Left = value; }
        }

        public double Top
        {
            get { return _settings.Top; }
            set { _settings.Top = value; }
        }

        public double FilePanelWidth
        {
            get { return _settings.FilePanelWidth; }
            set { _settings.FilePanelWidth = value; }
        }

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

        public bool UseFfmpeg
        {
            get { return _settings.UseFfmpeg; }
            set { _settings.UseFfmpeg = value; }
        }
    }

    public interface ISettings
    {
        bool IsMuted { get; set; }
        string ShadowplayPath { get; set; }
        bool ShowFileNames { get; set; }
        bool ShowPreviews { get; set; }
        double FilePanelWidth { get; set; }
        bool Maximized { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        double Left { get; set; }
        double Top { get; set; }
        bool UseFfmpeg { get; set; }
        void Save();
    }
}