using System;
using Foundation;
using Shiny.Jobs;
using UIKit;


namespace Shiny
{
    public static class iOSHooks
    {
        public static void ShinyFinishedLaunching(this UIApplicationDelegate app, IShinyStartup? startup = null)
            => ShinyHost.Init(new ApplePlatform(), startup);

        public static void ShinyDidReceiveRemoteNotification(this UIApplicationDelegate app, NSDictionary userInfo, Action<UIBackgroundFetchResult>? completionHandler)
            => ShinyHost.Resolve<AppleLifecycle>().DidReceiveRemoteNotification(userInfo, completionHandler);

        public static void ShinyRegisteredForRemoteNotifications(this UIApplicationDelegate app, NSData deviceToken)
            => ShinyHost.Resolve<AppleLifecycle>().RegisteredForRemoteNotifications(deviceToken);

        public static void ShinyFailedToRegisterForRemoteNotifications(this UIApplicationDelegate app, NSError error)
            => ShinyHost.Resolve<AppleLifecycle>().FailedToRegisterForRemoteNotifications(error);

        public static void ShinyPerformFetch(this UIApplicationDelegate app, Action<UIBackgroundFetchResult> completionHandler)
            => JobManager.OnBackgroundFetch(completionHandler);

        public static void ShinyHandleEventsForBackgroundUrl(this UIApplicationDelegate app, string sessionIdentifier, Action completionHandler)
            => ShinyHost.Resolve<AppleLifecycle>().HandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
    }
}