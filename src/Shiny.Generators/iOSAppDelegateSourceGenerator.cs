using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class iOSAppDelegateSourceGenerator : ShinyApplicationSourceGenerator
    {
        public iOSAppDelegateSourceGenerator() : base("UIKit.UIApplicationDelegate")
        {
        }


        protected override void Process(IEnumerable<INamedTypeSymbol> osAppTypeSymbols)
            => osAppTypeSymbols.ToList().ForEach(this.Process);


        void Process(INamedTypeSymbol appDelegate)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Foundation", "UIKit");

            using (builder.BlockInvariant($"namespace {appDelegate.ContainingNamespace}"))
            {
                using (builder.BlockInvariant($"public partial class {appDelegate.Name}"))
                {
                    this.GenerateFinishedLaunching(appDelegate, builder);
                    this.TryAppendPush(builder);

                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny",
                        "PerformFetch",
                        "public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyPerformFetch(completionHandler);"
                    );
                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny.Net.Http",
                        "HandleEventsForBackgroundUrl",
                        "public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);"
                    );
                }
            }
            this.Context.Source(builder.ToString(), appDelegate.Name);
        }


        void TryAppendPush(IndentedStringBuilder builder)
        {
            var hasPush = this.Context
                .Compilation
                .ReferencedAssemblyNames
                .Any(x =>
                    x.Name.StartsWith("Shiny.Push") &&
                    !x.Name.Equals("Shiny.Push.Abstractions")
                );

            if (hasPush)
            {
                builder.AppendLineInvariant("public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);");
                builder.AppendLineInvariant("public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) => this.ShinyDidReceiveRemoteNotification(userInfo, null);");
                builder.AppendLineInvariant("public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);");
                builder.AppendLineInvariant("public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);");
            }
        }

        void AppendMethodIf(INamedTypeSymbol symbol, IndentedStringBuilder builder, string neededLibrary, string methodName, string append)
        {
            var hasAssembly = this.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals(neededLibrary));
            if (!hasAssembly)
                return;

            var exists = symbol.HasMethod(methodName);

            if (exists)
            {
                this.Context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ShinyAppDelegateFinishedLaunchingExists",
                            "ShinyAppDelegate",
                            null,
                            "Shiny",
                            DiagnosticSeverity.Warning,
                            true
                        ),
                        Location.None
                    )
                );
            }
            else
            {
                builder.AppendLineInvariant(append);
            }
        }


        void GenerateFinishedLaunching(INamedTypeSymbol appDelegate, IndentedStringBuilder builder)
        {
            var exists = appDelegate.HasMethod("FinishedLaunching");
            if (exists)
            {
                this.Context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ShinyAppDelegateFinishedLaunchingExists",
                            "ShinyAppDelegate",
                            null,
                            "Shiny",
                            DiagnosticSeverity.Warning,
                            true
                        ),
                        appDelegate
                            .GetMembers()
                            .OfType<IMethodSymbol>()
                            .FirstOrDefault(x => x.Name.Equals("FinishedLaunching"))?
                            .Locations
                            .FirstOrDefault() ?? Location.None
                    )
                );
            }
            else
            {
                builder.AppendLineInvariant("partial void OnFinishedLaunching();");
                using (builder.BlockInvariant("public override bool FinishedLaunching(UIApplication app, NSDictionary options)"))
                {
                    builder.AppendLineInvariant("this.OnFinishedLaunching(app, options);");
                    builder.AppendLineInvariant($"this.ShinyFinishedLaunching(new {this.ShinyConfig.ShinyStartupTypeName}());");

                    this.TryAppendThirdParty(appDelegate, builder);
                    builder.AppendLineInvariant("return base.FinishedLaunching(app, options);");
                }
            }
        }


        void TryAppendThirdParty(INamedTypeSymbol appDelegate, IndentedStringBuilder builder)
        {
            var xfFormsDelegate = this.Context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Platform.iOS.FormsApplicationDelegate");
            if (xfFormsDelegate != null && appDelegate.Inherits(xfFormsDelegate))
            {
                // do XF stuff
                builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init();");
                builder.AppendLineInvariant($"this.LoadApplication(new {this.ShinyConfig.XamarinFormsAppTypeName}());");
            }
        }
    }
}