using System;
using DryIoc;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Shiny;
using Xamarin.Forms;


namespace $safeprojectname$
{
    public partial class App : PrismApplication
    {
        protected override async void OnInitialized()
        {
            this.InitializeComponent();
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewModelTypeName = viewType.FullName.Replace("Page", "ViewModel");
                var viewModelType = Type.GetType(viewModelTypeName);
                return viewModelType;
            });
            await this.NavigationService.Navigate("NavigationPage/StartupPage");
        }


        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<StartupPage>();
        }


        protected override IContainerExtension CreateContainerExtension()
        {
            var container = new Container(Rules
                .Default
                .WithAutoConcreteTypeResolution()
                .With(Made.Of(FactoryMethod.ConstructorWithResolvableArguments))
                .WithoutThrowOnRegisteringDisposableTransient()
                .WithoutFastExpressionCompiler()
                .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace)
            );
            ShinyHost.Populate((serviceType, func, lifetime) =>
                container.RegisterDelegate(
                    serviceType,
                    _ => func(),
                    Reuse.Singleton // HACK: I know everything is singleton
                )
            );
            return new DryIocContainerExtension(container);
        }
    }
}
