using System;
using Prism.Ioc;
using Prism.DryIoc;
using Xamarin.Forms;


namespace Samples
{
    public partial class App : PrismApplication
    {
        protected override async void OnInitialized()
        {
            this.InitializeComponent();
        }


        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>("Nav");
        }


        //protected override IContainerExtension CreateContainerExtension()
        //{
        //    var container = new Container(this.CreateContainerRules());
        //    ShinyHost.Populate((serviceType, func, lifetime) =>
        //        container.RegisterDelegate(
        //            serviceType,
        //            _ => func(),
        //            Reuse.Singleton // HACK: I know everything is singleton
        //        )
        //    );
        //    return new DryIocContainerExtension(container);
        //}
    }
}
