using Caliburn.Micro;
using Microsoft.Practices.Unity;

namespace ShadowClip.GUI
{
    public interface IDialogBuilder
    {
        void BuildDialog<T>(object data);
    }

    public class DialogBuilder : IDialogBuilder
    {
        private readonly IUnityContainer _container;
        private readonly IWindowManager _windowManager;

        public DialogBuilder(IWindowManager windowManager, IUnityContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        /*I'd like to get rid of this object type to retain type safety,
         but I can't figure out how to make C# generics and unity play togeather nicely.
         */

        public void BuildDialog<T>(object data)
        {
            var viewModel = _container.Resolve<T>(new ParameterOverrides {{"data", data}});
            _windowManager.ShowWindow(viewModel);
        }
    }
}