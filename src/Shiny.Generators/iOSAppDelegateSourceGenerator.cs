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

                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny.Push",
                        "ReceivedRemoteNotification",
                        "public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) => this.ShinyDidReceiveRemoteNotification(userInfo, null);"
                    );
                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny.Push",
                        "DidReceiveRemoteNotification",
                        "public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);"
                    );
                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny.Push",
                        "RegisteredForRemoteNotifications",
                        "public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);"
                    );
                    this.AppendMethodIf(
                        appDelegate,
                        builder,
                        "Shiny.Push",
                        "RegisteredForRemoteNotifications",
                        "public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);"
                    );

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


        void AppendMethodIf(INamedTypeSymbol symbol, IndentedStringBuilder builder, string neededLibrary, string methodName, string append)
        {
            var exists = symbol.HasMethod(methodName);

            if (exists)
            {
                // could check if the library is actually referenced?
                //this.Log.Warn($"Shiny generator could not add AppDelegate boilerplate method '{methodName}' because it already exists.  If you are using a '{neededLibrary}' that this is required, make sure you manually wire it up");
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
                //this.Log.Warn($"Shiny generator could not add AppDelegate.FinishedLaunching since it already exists.  You can remove this method and add 'OnFinishedLaunching' to do any additional custom setup");
            }
            else
            {
                using (builder.BlockInvariant("public override bool FinishedLaunching(UIApplication app, NSDictionary options)"))
                {
                    if (appDelegate.HasMethod("OnFinishedLaunching"))
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

            //// XF Material or RG Popups
            //if (this.Context.Compilation.GetTypeByMetadataName("XF.Material.Forms.Material") != null)
            //    builder.AppendLineInvariant("global::XF.Material.iOS.Material.Init();");
            //else if (this.Context.Compilation.GetTypeByMetadataName("Rg.Plugins.Popup.Popup") != null)
            //    builder.AppendLineInvariant("global::Rg.Plugins.Popup.Popup.Init();");

            //// AiForms.SettingsView
            //if (this.Context.Compilation.GetTypeByMetadataName("AiForms.Renderers.iOS.SettingsViewInit") != null)
            //    builder.AppendLineInvariant("global::AiForms.Renderers.iOS.SettingsViewInit.Init();");
        }
    }
}