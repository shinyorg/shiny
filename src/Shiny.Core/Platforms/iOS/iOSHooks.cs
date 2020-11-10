using System;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using UIKit;


namespace Shiny
{
    public static class iOSHooks
    {
        public static void ShinyFinishedLaunching(this UIApplicationDelegate app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => ShinyHost.Init(new ApplePlatformInitializer(), startup, platformBuild);

        public static void ShinyDidReceiveRemoteNotification(this UIApplicationDelegate app, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
            => ShinyHost.Container.RunDelegates<IAppDelegatePushNotificationHandler>(x => x.DidReceiveRemoteNotification(userInfo, completionHandler));

        public static void ShinyRegisteredForRemoteNotifications(this UIApplicationDelegate app, NSData deviceToken)
            => ShinyHost.Container.RunDelegates<IAppDelegatePushNotificationHandler>(x => x.RegisteredForRemoteNotifications(deviceToken));

        public static void ShinyFailedToRegisterForRemoteNotifications(this UIApplicationDelegate app, NSError error)
            => ShinyHost.Container.RunDelegates<IAppDelegatePushNotificationHandler>(x => x.FailedToRegisterForRemoteNotifications(error));

        public static void ShinyPerformFetch(this UIApplicationDelegate app, Action<UIBackgroundFetchResult> completionHandler)
            => JobManager.OnBackgroundFetch(completionHandler);

        public static void ShinyHandleEventsForBackgroundUrl(this UIApplicationDelegate app, string sessionIdentifier, Action completionHandler)
            => ShinyHost.Container.RunDelegates<IAppDelegateBackgroundUrlHandler>(x => x.HandleEventsForBackgroundUrl(sessionIdentifier, completionHandler));
    }
}