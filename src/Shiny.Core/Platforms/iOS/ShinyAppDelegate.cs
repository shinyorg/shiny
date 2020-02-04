using System;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;


namespace Shiny
{
    public class ShinyAppDelegate<T> : UIApplicationDelegate where T : IShinyStartup, new()
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            iOSShinyHost.Init(new T(), this.RegisterPlatformServices);
            return base.FinishedLaunching(app, options);
        }

        protected virtual void RegisterPlatformServices(IServiceCollection services) { }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
            => iOSShinyHost.RegisteredForRemoteNotifications(deviceToken);

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
            => iOSShinyHost.FailedToRegisterForRemoteNotifications(error);

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
            => iOSShinyHost.PerformFetch(completionHandler);

        public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
            => iOSShinyHost.HandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
    }
}
