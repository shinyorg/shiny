using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Shiny;

[assembly: ShinyApplication(ShinyStartupTypeName = "Samples.SampleStartup")]

namespace Samples.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        //public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        //{
        //    // this needs to be loaded before EVERYTHING
        //    this.ShinyFinishedLaunching(new SampleStartup());
        //    Forms.SetFlags(
        //        "SwipeView_Experimental",
        //        "Expander_Experimental",
        //        "RadioButton_Experimental"
        //    );
        //    Forms.Init();
        //    XF.Material.iOS.Material.Init();
        //    this.LoadApplication(new App());
        //    return base.FinishedLaunching(app, options);
        //}


        //#if !PRODUCTION
        //// these are generated in the main sample
        //// These methods will automatically be created and wired for you, as long as
        //// 1. You install Shiny.Core in this project
        //// 2. You don't customize them (meaning you don't implement these yourself)
        //// 3. This class is marked as partial

        //public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        //    => this.ShinyDidReceiveRemoteNotification(userInfo, null);

        //public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        //    => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);

        //public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        //    => this.ShinyRegisteredForRemoteNotifications(deviceToken);

        //public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        //    => this.ShinyFailedToRegisterForRemoteNotifications(error);

        //public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        //    => this.ShinyPerformFetch(completionHandler);

        //public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        //    => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

        //#endif
    }
}
