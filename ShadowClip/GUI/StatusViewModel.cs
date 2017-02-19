using System;
using System.Deployment.Application;
using System.Threading.Tasks;
using Caliburn.Micro;

// ReSharper disable LocalizableElement

namespace ShadowClip.GUI
{
    public class StatusViewModel : Screen
    {
        private readonly IDialogBuilder _dialogBuilder;

        public StatusViewModel(IDialogBuilder dialogBuilder)
        {
            _dialogBuilder = dialogBuilder;
        }

        public bool UpdateAvailable { get; set; }

        public Version AvailableVersion { get; set; }

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
            _dialogBuilder.BuildDialog<UpdateViewModel>(AvailableVersion);
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
                        AvailableVersion = info.AvailableVersion;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                });
        }
    }
}