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
        var host = this.CreateShinyHost();
        host.Run();

        Host.Lifecycle.FinishedLaunching(launchOptions);
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void WillEnterForeground(UIApplication application)
        => Host.Lifecycle.OnAppForegrounding();

    public override void DidEnterBackground(UIApplication application)
        => Host.Lifecycle.OnAppBackgrounding();

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => Host.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken);

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        => Host.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        => Host.Lifecycle.OnDidReceiveRemoteNotification(userInfo, completionHandler);
}