using System;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators.iOS
{
    public static class AppDelegateBoilerplateGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            // if application exists, error or override? - could also search for attribute?
            var appDelegateClass = context.Compilation.GetTypeByMetadataName("UIKit.UIApplicationDelegate");
            if (appDelegateClass == null)
                return;

            // find class in head project that inherits it

            // make sure it is partial

            var builder = new IndentedStringBuilder();

            using (builder.BlockInvariant("namespace ")) // match it
            {
                using (builder.BlockInvariant("public partial class YourAppDelegate : TheInheritAppDelegateType"))
                {
                    // TODO: could override/inherit user appdelegate to be able to weave in startup
                    builder.AppendLine("public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) => this.ShinyDidReceiveRemoteNotification(userInfo, null);");
                    builder.AppendLine("public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);");
                    builder.AppendLine("public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);");
                    builder.AppendLine("public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);");
                    builder.AppendLine("public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyPerformFetch(completionHandler)");
                    builder.AppendLine("public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);");
                }
            }
            context.AddCompilationUnit("AppDelegate", builder.ToString());
        }
    }
}
//public override bool FinishedLaunching(UIApplication app, NSDictionary options)
//{
//    // this needs to be loaded before EVERYTHING
//    this.ShinyFinishedLaunching(new SampleStartup());
//    return base.FinishedLaunching(app, options);
//}