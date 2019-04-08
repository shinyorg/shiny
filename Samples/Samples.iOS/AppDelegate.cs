using System;
using Shiny;
using Acr.UserDialogs;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Samples.ShinySetup;


namespace Samples.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // this needs to be loaded before EVERYTHING
            iOSShinyHost.Init(new SampleStartup());

            Forms.Init();
            this.LoadApplication(new App());
            return base.FinishedLaunching(app, options);
        }


        // if you are using jobs, you need this
        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
            => iOSShinyHost.OnBackgroundFetch(completionHandler);


        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
            => UserDialogs.Instance.Alert(notification.AlertBody, "Notification - " + notification.AlertTitle);
    }
}
