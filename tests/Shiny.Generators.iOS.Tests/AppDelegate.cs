using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: Shiny.GenerateStartup]
[assembly: Shiny.GenerateStaticClasses]


namespace Shiny.Generators.iOS.Tests
{
    [Register("MyAppDelegate")]
    public partial class MyAppDelegate : FormsApplicationDelegate
    {
        void OnFinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.SetFlags(
                "SwipeView_Experimental",
                "Expander_Experimental"
            );
            //this.LoadApplication(new App());
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) {}
        //public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) { }
        //public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {}
        //public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) {}
        //public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) {}
        //public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) { }
    }
}
