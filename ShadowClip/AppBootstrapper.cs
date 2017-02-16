using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Practices.Unity;
using ShadowClip.GUI;

namespace ShadowClip
{
    public class AppBootstrapper : BootstrapperBase
    {
        private readonly IUnityContainer _container = new UnityContainer();

        public AppBootstrapper()
        {
            Initialize();
        }


        protected override void Configure()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()) == false)
            {
                _container.RegisterTypes(
                    AllClasses.FromAssembliesInBasePath(),
                    WithMappings.FromMatchingInterface,
                    WithName.Default,
                    WithLifetime.Transient);

                Singleton<IWindowManager, WindowManager>();
                Singleton<IEventAggregator, EventAggregator>();
                Singleton<ISettings, Settings>();
            }
        }


        protected override object GetInstance(Type type, string name)
        {
            if (name != null)
                return _container.Resolve(type, name);
            return _container.Resolve(type);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.ResolveAll(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }

        private void Singleton<TFrom, TTo>() where TTo : TFrom
        {
            _container.RegisterType<TFrom, TTo>(new ContainerControlledLifetimeManager());
        }

        private void Singleton<TFrom>()
        {
            _container.RegisterType<TFrom>(new ContainerControlledLifetimeManager());
        }

        private void PerResolve<T>()
        {
            _container.RegisterType<T>(new PerResolveLifetimeManager());
        }
    }
}