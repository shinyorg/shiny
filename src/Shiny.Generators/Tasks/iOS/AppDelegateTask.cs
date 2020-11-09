using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks.iOS
{
    public class AppDelegateTask : ShinySourceGeneratorTask
    {
        string? shinyStartupClassName;


        public override void Execute()
        {
            if (!this.Context.IsIosAppProject())
                return;

            this.shinyStartupClassName = this.ShinyContext.GetShinyStartupClassFullName();
            if (this.shinyStartupClassName == null)
                return;

            var appDelegates = this
                .Context
                .GetAllDerivedClassesForType("UIKit.UIApplicationDelegate")
                .WhereNotSystem()
                .ToList();

            foreach (var appDelegate in appDelegates)
                this.BuildDelegate(appDelegate);
        }


        void BuildDelegate(INamedTypeSymbol appDelegate)
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
                        "Shiny.Core",
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
            this.Context.AddCompilationUnit(appDelegate.Name, builder.ToString());
        }


        void GenerateFinishedLaunching(INamedTypeSymbol appDelegate, IndentedStringBuilder builder)
        {
            var exists = appDelegate.HasMethod("FinishedLaunching");
            if (exists)
            {
                this.Log.Warn($"Shiny generator could not add AppDelegate.FinishedLaunching since it already exists.  You can remove this method and add 'OnFinishedLaunching' to do any additional custom setup");
            }
            else
            {
                using (builder.BlockInvariant("public override bool FinishedLaunching(UIApplication app, NSDictionary options)"))
                {
                    if (appDelegate.HasMethod("OnFinishedLaunching"))
                        builder.AppendLineInvariant("this.OnFinishedLaunching(app, options);");

                    builder.AppendLineInvariant($"this.ShinyFinishedLaunching(new {this.shinyStartupClassName}());");

                    this.TryAppendThirdParty(appDelegate, builder);
                    builder.AppendLineInvariant("return base.FinishedLaunching(app, options);");
                }
            }
        }


        void TryAppendThirdParty(INamedTypeSymbol appDelegate, IIndentedStringBuilder builder)
        {
            // Xamarin Forms
            if (appDelegate.Is("Xamarin.Forms.Platform.iOS.FormsApplicationDelegate"))
            {
                var appClass = this.ShinyContext.GetXamFormsAppClassFullName();
                if (appClass != null)
                {
                    // TODO: what if app class has multiple ctor args - skip
                    builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init();");
                    builder.AppendLineInvariant($"this.LoadApplication(new {appClass}());");
                }
            }

            //// ZXing.Net.Mobile
            //if (this.Context.HasZXingNetMobile())
            //    builder.AppendLineInvariant("global::ZXing.Net.Mobile.Forms.iOS.Platform.Init();");

            // XF Material or RG Popups
            if (this.Context.Compilation.GetTypeByMetadataName("XF.Material.Forms.Material") != null)
                builder.AppendLineInvariant("global::XF.Material.iOS.Material.Init();");
            else if (this.Context.Compilation.GetTypeByMetadataName("Rg.Plugins.Popup.Popup") != null)
                builder.AppendLineInvariant("global::Rg.Plugins.Popup.Popup.Init();");

            // AiForms.SettingsView
            if (this.Context.Compilation.GetTypeByMetadataName("AiForms.Renderers.iOS.SettingsViewInit") != null)
                builder.AppendLineInvariant("global::AiForms.Renderers.iOS.SettingsViewInit.Init();");


        }


        void AppendMethodIf(INamedTypeSymbol symbol, IndentedStringBuilder builder, string neededLibrary, string methodName, string append)
        {
            var exists = symbol.HasMethod(methodName);

            if (exists)
            {
                // could check if the library is actually referenced?
                this.Log.Warn($"Shiny generator could not add AppDelegate boilerplate method '{methodName}' because it already exists.  If you are using a '{neededLibrary}' that this is required, make sure you manually wire it up");
            }
            else
            {
                builder.AppendLineInvariant(append);
            }
        }
    }
}