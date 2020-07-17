using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators.iOS
{
    public static class AppDelegateBoilerplateGenerator
    {
        static ISourceGeneratorLogger log;


        public static void Execute(SourceGeneratorContext context)
        {
            log = context.GetLogger();
            var appDelegateClass = context.Compilation.GetTypeByMetadataName("UIKit.UIApplicationDelegate");
            if (appDelegateClass == null)
                return;

            //var startupClass = context.GetShinyStartupSymbol();
            //if (startupClass == null)
            //    return;

            var appDelegates = context
                .GetAllDerivedClassesForType("UIKit.UIApplicationDelegate")
                .WhereNotSystem()
                .ToList();

            //System.Diagnostics.Debugger.Launch();
            foreach (var appDelegate in appDelegates)
            {
                var builder = new IndentedStringBuilder();
                builder.AppendNamespaces("Foundation", "UIKit");

                using (builder.BlockInvariant($"namespace {appDelegate.ContainingNamespace}"))
                {
                    //using (builder.BlockInvariant($"public partial class {appDelegate.Name} : {appDelegate.BaseType.ToDisplayString()}"))
                    using (builder.BlockInvariant($"public partial class {appDelegate.Name}"))
                    {
                        AppendMethodIf(
                            appDelegate, 
                            builder,
                            "Shiny.Push",
                            "ReceivedRemoteNotification",
                            "public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) => this.ShinyDidReceiveRemoteNotification(userInfo, null);"
                        );
                        AppendMethodIf(
                            appDelegate,
                            builder,
                            "Shiny.Push",
                            "DidReceiveRemoteNotification",
                            "public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);"
                        );
                        AppendMethodIf(
                            appDelegate,
                            builder,
                            "Shiny.Push",
                            "RegisteredForRemoteNotifications",
                            "public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);"
                        );
                        AppendMethodIf(
                            appDelegate,
                            builder,
                            "Shiny.Push",
                            "RegisteredForRemoteNotifications",
                            "public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);"
                        );

                        AppendMethodIf(
                            appDelegate,
                            builder,
                            "Shiny.Core",
                            "PerformFetch",
                            "public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyPerformFetch(completionHandler);"
                        );
                        AppendMethodIf(
                            appDelegate,
                            builder,
                            "Shiny.Net.Http",
                            "HandleEventsForBackgroundUrl",
                            "public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);"
                        );
                    }
                }
                context.AddCompilationUnit(appDelegate.Name, builder.ToString());
            }
        }


        static void AppendMethodIf(INamedTypeSymbol symbol, IIndentedStringBuilder builder, string neededLibrary, string methodName, string append)
        {
            var exists = symbol
                .GetTypeMembers()
                .OfType<IMethodSymbol>()
                .Any(x => x.Name.Equals(methodName));

            if (exists)
            {
                // could check if the library is actually referenced?
                log.Warn($"Shiny generator could not add AppDelegate boilerplate method '{methodName}' because it already exists.  If you are using a '{neededLibrary}' that this is required, make sure you manually wire it up");
            }
            else
            {
                builder.AppendLineInvariant(append);
            }
        }
    }
}