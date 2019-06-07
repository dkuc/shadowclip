using System.Windows;
using System.Windows.Interop;

namespace ShadowClip.GUI
{
    partial class ShellView
    {
        private static HwndTarget _hwndTarget;

        public static void EnableSoftwareRender()
        {
            _hwndTarget.RenderMode = RenderMode.SoftwareOnly;
        }

        public static void EnableDefaultRender()
        {
            _hwndTarget.RenderMode = RenderMode.Default;
        }

        private void ShellView_OnLoaded(object sender, RoutedEventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            _hwndTarget = hwndSource.CompositionTarget;
        }
    }
}