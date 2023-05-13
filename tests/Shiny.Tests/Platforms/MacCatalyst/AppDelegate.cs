using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using UIKit;
using Shiny.Hosting;

namespace Shiny.Tests;


[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    // public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    //     => Host.Lifecyle.OnRegisteredForRemoteNotifications(deviceToken);
    //
    // [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    // public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    //     => Host.Lifecyle.OnFailedToRegisterForRemoteNotifications(error);
    //
    // [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    // public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //     => Host.Lifecyle.OnDidReceiveRemoveNotification(userInfo, completionHandler);
}