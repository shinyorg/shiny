using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Shiny.Hosting;
#if APPLE
using Foundation;
using ObjCRuntime;
using UIKit;
#endif


namespace Shiny;


public abstract class ShinyUnoApp : Application
{

#if APPLE
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        => Host.Lifecycle.OnContinueUserActivity(userActivity, completionHandler);

    public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        => Host.Lifecycle.OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

    //[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    //public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    //{
    //    throw new System.NotImplementedException();
    //}

    //public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //{
    //    base.DidReceiveRemoteNotification(application, userInfo, completionHandler);
    //}
#elif ANDROID

    // TODO: I need lifecycle against mainactivity
     // Shiny will supply app foreground/background events
                //.OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Lifecycle.OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
                //.OnActivityResult((activity, requestCode, result, intent) => Host.Lifecycle.OnActivityResult(activity, requestCode, result, intent))
                //.OnNewIntent((activity, intent) => Host.Lifecycle.OnNewIntent(activity, intent))
#endif
}