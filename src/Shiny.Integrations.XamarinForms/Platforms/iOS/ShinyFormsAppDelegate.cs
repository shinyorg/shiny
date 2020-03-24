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

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
            => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
            => this.ShinyRegisteredForRemoteNotifications(deviceToken);

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
            => this.ShinyFailedToRegisterForRemoteNotifications(error);

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
            => this.ShinyPerformFetch(completionHandler);

        public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
            => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
    }
}
