using System;
using System.Diagnostics;
using Foundation;
using Microsoft.Extensions.Options;
using ObjCRuntime;
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

        host.Lifecycle().FinishedLaunching(launchOptions);
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void WillEnterForeground(UIApplication application)
        => Host.Current.Lifecycle().OnAppForegrounding();

    public override void DidEnterBackground(UIApplication application)
        => Host.Current.Lifecycle().OnAppBackgrounding();

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => Host.Current.Lifecycle().OnRegisteredForRemoteNotifications(deviceToken);

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        => Host.Current.Lifecycle().OnFailedToRegisterForRemoteNotifications(error);

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        => Host.Current.Lifecycle().OnDidReceiveRemoveNotification(userInfo, completionHandler);
}