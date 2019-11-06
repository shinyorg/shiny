using System;
using System.Reflection;
using Foundation;
using UIKit;
using Xunit.Runner;
using Xunit.Sdk;


namespace Shiny.Device.Tests.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : RunnerAppDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //Acr.Logging.Log.ToDebug();
            iOSShinyHost.Init(new TestStartup());
            this.AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            this.AddTestAssembly(typeof(TestStartup).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            this.AutoStart = false;
            this.TerminateAfterExecution = false;
            //[assembly: CollectionBehavior(MaxParallelThreads = n)]

            return base.FinishedLaunching(app, options);
        }
    }
}
