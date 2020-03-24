﻿using System;
using Dev_Template.Infrastructure;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Shiny;
using Shiny.Jobs;


namespace $safeprojectname$
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // this needs to be loaded before EVERYTHING
            this.ShinyFinishedLaunching(new ShinyStartup());

            Forms.Init();
            Rg.Plugins.Popup.Popup.Init();
            this.LoadApplication(new App());
            return base.FinishedLaunching(app, options);
        }


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
