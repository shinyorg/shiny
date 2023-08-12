using System;
using Foundation;
using Shiny.Hosting;
using UIKit;

namespace Shiny;


public abstract class ShinyAppDelegate : UIApplicationDelegate
{
    protected abstract IHost CreateShinyHost();


    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        this.CreateShinyHost().Run();
        return base.FinishedLaunching(application, launchOptions);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        => Host.Lifecycle.OnContinueUserActivity(userActivity,  completionHandler);

    public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        => Host.Lifecycle.OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => Host.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken);

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        => Host.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        => Host.Lifecycle.OnDidReceiveRemoteNotification(userInfo, completionHandler);
}