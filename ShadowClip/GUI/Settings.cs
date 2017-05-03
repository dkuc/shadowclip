using System;
using System.Windows.Navigation;
using ShadowClip.services;

namespace ShadowClip.GUI
{
    internal class Settings : ISettings
    {
        private readonly Properties.Settings _settings = new Properties.Settings();

        public bool Maximized
        {
            get => _settings.Maximized;
            set => _settings.Maximized = value;
        }

        public double Width
        {
            get => _settings.Width;
            set => _settings.Width = value;
        }

        public double Height
        {
            get => _settings.Height;
            set => _settings.Height = value;
        }

        public double Left
        {
            get => _settings.Left;
            set => _settings.Left = value;
        }

        public double Top
        {
            get => _settings.Top;
            set => _settings.Top = value;
        }

        public double FilePanelWidth
        {
            get => _settings.FilePanelWidth;
            set => _settings.FilePanelWidth = value;
        }

        public bool ShowFileNames
        {
            get => _settings.ShowFileNames;
            set => _settings.ShowFileNames = value;
        }

        public bool ShowPreviews
        {
            get => _settings.ShowPreviews;
            set => _settings.ShowPreviews = value;
        }

        public string ShadowplayPath
        {
            get => _settings.ShadowplayPath;
            set => _settings.ShadowplayPath = value;
        }

        public bool IsMuted
        {
            get => _settings.IsMuted;
            set => _settings.IsMuted = value;
        }

        public void Save()
        {
            _settings.Save();
        }

        public bool UseFfmpeg
        {
            get => _settings.UseFfmpeg;
            set => _settings.UseFfmpeg = value;
        }

        public Destination Destination
        {
            get
            {
                Enum.TryParse(_settings.Destination, out Destination destination);
                return destination;
            }
            set => _settings.Destination = value.ToString();
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
        Destination Destination { get; set; }
        void Save();
    }
}