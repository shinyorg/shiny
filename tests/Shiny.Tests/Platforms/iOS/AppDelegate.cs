using Foundation;
using UIKit;

namespace Shiny.Tests;


[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();


    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) { }


    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) { }


    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) { }
}