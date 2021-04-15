using System;
using Foundation;
using UIKit;
using Xunit.Runner;
using Xunit.Sdk;

[assembly: global::Shiny.ShinyApplication(ShinyStartupTypeName = "Shiny.Tests.TestStartup")]


namespace Shiny.Tests.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : RunnerAppDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            this.ShinyFinishedLaunching(new TestStartup());
            this.AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            this.AddTestAssembly(this.GetType().Assembly);
            this.AddTestAssembly(typeof(TestStartup).Assembly);

            this.AutoStart = false;
            this.TerminateAfterExecution = false;
            //[assembly: CollectionBehavior(MaxParallelThreads = n)]

            return base.FinishedLaunching(app, options);
        }


        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            this.ShinyRegisteredForRemoteNotifications(deviceToken);
        }


        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            this.ShinyFailedToRegisterForRemoteNotifications(error);
        }


        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);
        }
    }
}
