using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Xamarin.Forms;
using Application = Windows.UI.Xaml.Application;
using Frame = Windows.UI.Xaml.Controls.Frame;
using Shiny;


namespace Samples.UWP
{
    sealed partial class App : Application
    {
        public App()
        {
            //app.EnteredBackground
            //app.LeavingBackground
            //app.Suspending

            this.InitializeComponent();
            //UwpShinyHost.ClearBackgroundTasks();
            this.ShinyInit(new SampleStartup());
        }


        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += this.OnNavigationFailed;
                Rg.Plugins.Popup.Popup.Init();
                Forms.SetFlags(
                    "SwipeView_Experimental",
                    "Expander_Experimental",
                    "RadioButton_Experimental"
                );
                Forms.Init(e);

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
                rootFrame.Navigate(typeof(MainPage), e.Arguments);

            Window.Current.Activate();
        }


        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
            => throw new Exception("Failed to load Page " + e.SourcePageType.FullName);


        void OnSuspending(object sender, SuspendingEventArgs e) => e
            .SuspendingOperation
            .GetDeferral()
            .Complete();
    }
}
