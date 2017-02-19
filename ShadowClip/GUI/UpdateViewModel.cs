using System;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Screen = Caliburn.Micro.Screen;

namespace ShadowClip.GUI
{
    public sealed class UpdateViewModel : Screen
    {
        public Version NewVersion { get; }
        public string ReleaseNotes { get; set; }

        public UpdateViewModel(Version data)
        {
            NewVersion = data;
            DisplayName = $"New Update: {data}";
        }

        protected override void OnViewLoaded(object view)
        {
            GetReleaseNotes();
        }

        private async void GetReleaseNotes()
        {
            try
            {
                ReleaseNotes = "Loading...";
                var originalLength = File.ReadAllLines("release_notes.txt").Length;
                var newVersion = NewVersion.ToString().Replace('.', '_');
                var url = ApplicationDeployment.CurrentDeployment.UpdateLocation;
                string baseUrl = url.AbsoluteUri.Remove(url.AbsoluteUri.Length - url.Segments.Last().Length);

                var notesUri = $"{baseUrl}Application%20Files/ShadowClip_{newVersion}/release_notes.txt.deploy";
                HttpClient client = new HttpClient();
                var newReleaseNotes = await client.GetStringAsync(notesUri);
                var lines = newReleaseNotes.Split(Environment.NewLine.ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries);
                var newLineCount = lines.Length - originalLength;
                var notesSinceLastUpdate = string.Join(Environment.NewLine, lines.Take(newLineCount));
                ReleaseNotes = notesSinceLastUpdate;
            }
            catch (Exception e)
            {
                ReleaseNotes = $"Error Loading Release Notes: {e.Message}";
            }
        }

        public void Cancel()
        {
            this.TryClose();
        }

        public void UpdateNow()
        {
            if (ApplicationDeployment.IsNetworkDeployed == false)
                return;

            var ad = ApplicationDeployment.CurrentDeployment;


            try
            {
                ad.Update();
                Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
            catch (DeploymentDownloadException dde)
            {
                MessageBox.Show("Error updating app: " + dde);
            }
        }
    }
}