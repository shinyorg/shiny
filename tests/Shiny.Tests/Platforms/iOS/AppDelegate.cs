using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using UIKit;

namespace Shiny.Tests;


[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    //[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    //public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    //    => global::Shiny.Hosting.Host.Current.Lifecycle().OnRegisteredForRemoteNotifications(deviceToken);

    //[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    //public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    //    => global::Shiny.Hosting.Host.Current.Lifecycle().OnFailedToRegisterForRemoteNotifications(error);

    //[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    //public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //    => global::Shiny.Hosting.Host.Current.Lifecycle().OnDidReceiveRemoveNotification(userInfo, completionHandler);
}