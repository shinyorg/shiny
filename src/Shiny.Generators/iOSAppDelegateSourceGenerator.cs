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
        {
            var list = osAppTypeSymbols.ToList();

            if (list.Any())
            {
                osAppTypeSymbols.ToList().ForEach(this.Process);
            }
            else
            {
                this.Context.Log(
                    "SHINY004",
                    "No AppDelegate Found"
                );
            }
        }


        void Process(INamedTypeSymbol appDelegate)
        {
            this.Context.Log(
                "SHINY003",
                $"Generating AppDelegate Boilerplate for '{appDelegate.ToDisplayString()}'",
                DiagnosticSeverity.Info
            );

            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Foundation", "UIKit");

            using (builder.BlockInvariant($"namespace {appDelegate.ContainingNamespace}"))
            {
                using (builder.BlockInvariant($"public partial class {appDelegate.Name}"))
                {
                    this.GenerateFinishedLaunching(appDelegate, builder);
                    this.TryAppendPush(builder, appDelegate);

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

                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Xamarin.Essentials",
                        "PerformActionForShortcutItem",
                        "public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler) => Xamarin.Essentials.Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);"
                    );

                    if (this.Context.HasMsal())
                    {
                        this.AppendMethodIf(
                            appDelegate,
                            builder,
                            "Microsoft.Identity.Client",
                            "OpenUrl",
@"
public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
{
    if (global::Microsoft.Identity.Client.AuthenticationContinuationHelper.IsBrokerResponse(sourceApplication))
    {
        global::Microsoft.Identity.Client.AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url);
        return true;
    }

    if (global::Microsoft.Identity.Client.AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url))
    {
        return true;
    }
    return base.OpenUrl(application, url, sourceApplication, annotation);
}"
                        );
                    }
                    else
                    {
                        this.AppendMethodIf(
                            appDelegate,
                            builder,
                            "Xamarin.Essentials",
                            "OpenUrl",
@"
public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
{
    if (global::Xamarin.Essentials.Platform.OpenUrl(app, url, options))
        return true;

    return base.OpenUrl(app, url, options);
}"
                        );
                    }
                }
            }
            this.Context.AddSource(appDelegate.Name, builder.ToString());
        }


        void TryAppendPush(IndentedStringBuilder builder, INamedTypeSymbol appDelegate)
        {
            var hasPush = this.Context
                .Compilation
                .ReferencedAssemblyNames
                .Any(x => x.Name.StartsWith("Shiny.Push"));

            if (hasPush)
            {
                this.AppendMethodIf(
                    appDelegate,
                    builder,
                    "RegisteredForRemoteNotifications",
                    "public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);"
                );
                this.AppendMethodIf(
                    appDelegate,
                    builder,
                    "FailedToRegisterForRemoteNotifications",
                    "public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);"
                );
                this.AppendMethodIf(
                    appDelegate,
                    builder,
                    "DidReceiveRemoteNotification",
                    "public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);"
                );
            }
        }


        void AppendMethodIf(INamedTypeSymbol symbol, IndentedStringBuilder builder, string neededLibrary, string methodName, string append)
        {
            var hasAssembly = this.Context.HasReference(neededLibrary);
            if (!hasAssembly)
                return;

            this.AppendMethodIf(symbol, builder, methodName, append);
        }


        void AppendMethodIf(INamedTypeSymbol symbol, IndentedStringBuilder builder, string methodName, string append)
        {
            var exists = symbol.HasMethod(methodName);

            if (exists)
            {
                this.Context.Log(
                    "SHINY002",
                    $"Method '{methodName}' already exists on your appdelegate, make sure you call the Shiny hook for this"
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
                this.Context.Log(
                    "SHINY001",
                    "FinishedLaunching already exists on your appdelegate.  Make sure to call the this.ShinyFinishedLaunching(new YourStartup());",
                    DiagnosticSeverity.Warning,
                    appDelegate
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(x => x.Name.Equals("FinishedLaunching"))?
                        .Locations
                        .FirstOrDefault()
                );
            }
            else
            {
                builder.AppendLineInvariant("partial void OnPreFinishedLaunching(UIApplication app, NSDictionary options);");
                builder.AppendLineInvariant("partial void OnPostFinishedLaunching(UIApplication app, NSDictionary options);");
                using (builder.BlockInvariant("public override bool FinishedLaunching(UIApplication app, NSDictionary options)"))
                {
                    builder.AppendLineInvariant("this.OnPreFinishedLaunching(app, options);");
                    builder.AppendLineInvariant($"this.ShinyFinishedLaunching(new global::{this.ShinyConfig.ShinyStartupTypeName}());");

                    this.TryAppendThirdParty(appDelegate, builder);
                    builder.AppendLineInvariant("this.OnPostFinishedLaunching(app, options);");
                    builder.AppendLineInvariant("return base.FinishedLaunching(app, options);");
                }
            }
        }


        void TryAppendThirdParty(INamedTypeSymbol appDelegate, IndentedStringBuilder builder)
        {
            if (this.ShinyConfig.ExcludeThirdParty)
                return;

            // AiForms.SettingsView
            if (this.Context.Compilation.GetTypeByMetadataName("AiForms.Renderers.iOS.SettingsViewInit") != null)
                builder.AppendLineInvariant("global::AiForms.Renderers.iOS.SettingsViewInit.Init();");

            // XF Material & RG Popup
            if (this.Context.Compilation.GetTypeByMetadataName("XF.Material.iOS.Material") != null)
                builder.AppendLineInvariant("global::XF.Material.iOS.Material.Init();");
            else if (this.Context.Compilation.GetTypeByMetadataName("Rg.Plugins.Popup.Popup") != null)
                builder.AppendLineInvariant("Rg.Plugins.Popup.Popup.Init();");

            if (this.Context.Compilation.GetTypeByMetadataName("Xamarin.Forms.FormsMaterial") != null)
                builder.AppendLineInvariant("global::Xamarin.Forms.FormsMaterial.Init();");

            if (this.Context.HasMobileBuildToolsConfig())
                builder.AppendLineInvariant("global::Mobile.BuildTools.Configuration.ConfigurationManager.Init();");

            var xfFormsDelegate = this.Context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Platform.iOS.FormsApplicationDelegate");
            if (!String.IsNullOrWhiteSpace(this.ShinyConfig.XamarinFormsAppTypeName) &&
                xfFormsDelegate != null &&
                appDelegate.Inherits(xfFormsDelegate))
            {
                // do XF stuff
                builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init();");
                builder.AppendLineInvariant($"this.LoadApplication(new global::{this.ShinyConfig.XamarinFormsAppTypeName}());");
            }
        }
    }
}