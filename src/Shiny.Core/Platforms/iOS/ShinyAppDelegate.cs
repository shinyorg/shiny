using System;
using Foundation;
using UIKit;

namespace Shiny
{
    public class ShinyAppDelegate<T> : UIApplicationDelegate where T : IShinyStartup, new()
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            iOSShinyHost.Init(new T());
            return base.FinishedLaunching(app, options);
        }


        public override void OnActivated(UIApplication application)
            => iOSShinyHost.OnActivated();

        public override void OnResignActivation(UIApplication uiApplication)
            => iOSShinyHost.OnBackground();

        public override void WillTerminate(UIApplication uiApplication)
            => iOSShinyHost.OnTerminate();

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
