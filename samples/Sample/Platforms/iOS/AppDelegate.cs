using Foundation;
using UIKit;
using Shiny.Hosting;
using Shiny;

namespace Sample;


[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => Host.Current.Lifecycle().OnRegisteredForRemoteNotifications(deviceToken);

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        => Host.Current.Lifecycle().OnFailedToRegisterForRemoteNotifications(error);

    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        => Host.Current.Lifecycle().OnDidReceiveRemoveNotification(userInfo, completionHandler);

    [Export("application:handleEventsForBackgroundURLSession:completionHandler:")]
    public bool HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) 
        => Host.Current.Lifecycle().OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
}