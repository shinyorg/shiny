using System;
using System.Threading.Tasks;
using Prism.Navigation;


namespace $safeprojectname$
{
    public class StartupViewModel : ServicesViewModel
    {
        readonly INavigationService navigation;


        public StartupViewModel(ICoreServices core,
                                INavigationService navigation) : base(core)
        {
            this.navigation = navigation;
        }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            // var page = await this.authService.RenewSecurityIfApplicable()
            //     ? "app://NavigationPage/MainPage"
            //     : "WelcomePage";

            // await Task.Delay(3000);
            //await this.navigation.Navigate(page);
        }
    }
}
