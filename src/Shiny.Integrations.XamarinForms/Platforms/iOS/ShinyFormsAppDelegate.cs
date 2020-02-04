using System;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;


namespace Shiny
{
    public class ShinyFormsAppDelegate<T> : FormsApplicationDelegate where T : Application, IShinyStartup, new()
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var instance = new T();
            iOSShinyHost.Init(instance, this.RegisterPlatformServices);
            Forms.Init();
            this.LoadApplication(instance);

            return base.FinishedLaunching(app, options);
        }


        protected virtual void RegisterPlatformServices(IServiceCollection services) {}

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
