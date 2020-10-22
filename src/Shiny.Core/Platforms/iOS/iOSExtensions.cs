using System;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using ObjCRuntime;
using UIKit;


namespace Shiny
{
    public static class iOSExtensions
    {
        //public static bool IsSimulator
        //    => Runtime.Arch == Arch.SIMULATOR;

        public static void ShinyFinishedLaunching(this UIApplicationDelegate app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => ShinyHost.Init(new ApplePlatform(), startup, platformBuild);

        public static void ShinyDidReceiveRemoteNotification(this UIApplicationDelegate app, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) { }
        //=> ShinyHost.DidReceiveRemoteNotification(userInfo, completionHandler);

        public static void ShinyRegisteredForRemoteNotifications(this UIApplicationDelegate app, NSData deviceToken) { }
        //=> iOSShinyHost.RegisteredForRemoteNotifications(deviceToken);

        public static void ShinyFailedToRegisterForRemoteNotifications(this UIApplicationDelegate app, NSError error) { }
            //=> iOSShinyHost.FailedToRegisterForRemoteNotifications(error);

        public static void ShinyPerformFetch(this UIApplicationDelegate app, Action<UIBackgroundFetchResult> completionHandler) { }
        //=> iOSShinyHost.PerformFetch(completionHandler);

        public static void ShinyHandleEventsForBackgroundUrl(this UIApplicationDelegate app, string sessionIdentifier, Action completionHandler) { }
            //=> iOSShinyHost.HandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
    }
}
