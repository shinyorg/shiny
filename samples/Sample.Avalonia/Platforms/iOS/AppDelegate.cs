using Avalonia;
using Avalonia.iOS;
using Foundation;
using Shiny;
using Shiny.Hosting;
using UIKit;

namespace Sample.Avalonia;


[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    // Avalonia runs this from didFinishLaunching. Foreground/background and notification taps then reach Shiny
    // with no more code - IosLifecycleExecutor observes them itself.
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).UseShiny(ShinyStartup.Configure);


    // Shiny.Net.Http background transfers. Not a push method, so it is safe to have in any app.
    [Export("application:handleEventsForBackgroundURLSession:completionHandler:")]
    public void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        => Host.Lifecycle.OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

    // Shiny.Push - add these ONLY in an app that uses push. Implementing them tells App Review the app uses push.
    //
    // [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    // public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    //     => Host.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken);
    //
    // [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    // public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    //     => Host.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);
    //
    // [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    // public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //     => Host.Lifecycle.OnDidReceiveRemoteNotification(userInfo, completionHandler);
}
