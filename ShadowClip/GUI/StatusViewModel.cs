using System;
using System.Deployment.Application;
using System.Threading.Tasks;
using System.Windows.Forms;
using Screen = Caliburn.Micro.Screen;

// ReSharper disable LocalizableElement

namespace ShadowClip.GUI
{
    public class StatusViewModel : Screen
    {
        public bool UpdateAvailable { get; set; }

        public string Version
        {
            get
            {
                var isNetworkDeployed = ApplicationDeployment.IsNetworkDeployed;
                if (isNetworkDeployed)
                {
                    var applicationDeployment = ApplicationDeployment.CurrentDeployment;
                    var currentDeploymentCurrentVersion = applicationDeployment.CurrentVersion;
                    return $"V. {currentDeploymentCurrentVersion}";
                }
                return "V. 0";
            }
        }

        protected override void OnViewLoaded(object view)
        {
            CheckForUpdate();
        }

        public void OnUpdateClick()
        {
            if (ApplicationDeployment.IsNetworkDeployed == false)
                return;

            var ad = ApplicationDeployment.CurrentDeployment;
            var dr =
                MessageBox.Show(
                    "Would you like to automtically update the app now? If you click OK, the app will apear to freeze as it updates.",
                    "Update Available", MessageBoxButtons.OKCancel);
            if (DialogResult.OK != dr)
                return;


            try
            {
                ad.Update();
                Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
            catch (DeploymentDownloadException dde)
            {
                MessageBox.Show(
                    "Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " +
                    dde);
            }
        }

        private void CheckForUpdate()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
                Task.Run(() =>
                {
                    var ad = ApplicationDeployment.CurrentDeployment;

                    try
                    {
                        var info = ad.CheckForDetailedUpdate();
                        UpdateAvailable = info.UpdateAvailable;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                });
        }
    }
}