using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Devlead.Statiq.Tabs;
using Statiq.Common;
using Statiq.Markdown;


namespace Docs.Shortcodes
{
    public class PackageInfoShortcode : SyncShortcode
    {
        public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packageName = args.FirstOrDefault().Value;
            var package = Utils.GetPackage(packageName);
            var scount = package.Services?.Length ?? 0;
            var pcontent = "";

            switch (scount)
            {
                case 0:
                    throw new ArgumentException($"{packageName} has no services defined");

                case 1:
                    pcontent = RenderService(document, package, package.Services!.First());
                    break;

                default:
                    if (args.Length != 2)
                        throw new ArgumentException($"Package '{packageName}' has multiple services and thus requires a secondary argument of the service you want to render");

                    var sn = args[1].Value;
                    var service = package
                        .Services!
                        .FirstOrDefault(x => x.Name.Equals(sn));

                    if (service == null)
                        throw new ArgumentException("No service found called " + sn);

                    pcontent = RenderService(document, package, service);
                    break;
            }
            return new ShortcodeResult(pcontent);
        }


        //static void AppendIf(StringBuilder sb, string category, string? value)
        //{
        //    if (!value.IsNullOrEmpty())
        //        sb.AppendLine($"|**{category}**|{value}|");
        //}


        static string RenderService(IDocument document, Package package, PackageService service)
        {
            var tabGroup = BuildTabGroup(package, service);
            var contentBuilder = new StringBuilder();

            contentBuilder.AppendLine("<div class=\"tab-wrap\">");

            var first = true;
            foreach (var tab in tabGroup.Tabs)
            {
                contentBuilder.AppendLine($"<input type=\"radio\" id=\"{tabGroup.Id}-{tab.Id}\" name=\"{tabGroup.Id}\" class=\"tab\" {(first ? "checked" : string.Empty)}><label for=\"{tabGroup.Id}-{tab.Id}\" >{tab.Name}</label>");
                first = false;
            }

            foreach (var tab in tabGroup.Tabs)
            {
                contentBuilder.AppendLine("<div class=\"tab__content\">");
                contentBuilder.AppendLine();

                using (var writer = new StringWriter())
                {
                    MarkdownHelper.RenderMarkdown(
                        document,
                        tab.Content,
                        writer,
                        false, //prependLinkRoot
                        "common", //configuration,
                        null
                    );
                    contentBuilder.AppendLine(writer.ToString());
                }

                contentBuilder.AppendLine();
                contentBuilder.AppendLine("</div>");
            }

            contentBuilder.AppendLine("</div>");

            return contentBuilder.ToString();
        }


        static TabGroup BuildTabGroup(Package package, PackageService service)
        {
            var tabGroup = new TabGroup();

            var tabs = new List<TabGroupTab>();
            tabs.Add(new TabGroupTab
            {
                Name = "General",
                Content = RenderGeneralTab(package, service)
            });
            tabs.Add(new TabGroupTab
            {
                Name = "Startup",
                Content = RenderStartup(service)
            });

            if (service.Android != null)
            {
                tabs.Add(new TabGroupTab
                {
                    Name = "Android",
                    Content = RenderAndroid(service.Android)
                });
            }

            if (service.iOS != null)
            {
                tabs.Add(new TabGroupTab
                {
                    Name = "iOS",
                    Content = RenderIosTab(service.iOS)
                });
            }
            if (service.Uwp != null)
            {
                tabs.Add(new TabGroupTab
                {
                    Name = "Windows",
                    Content = RenderUwp(service.Uwp)
                });
            }
            tabGroup.Tabs = tabs.ToArray();
            return tabGroup;
        }


        static string RenderGeneralTab(Package package, PackageService service)
            => new StringBuilder()
                .AppendLine("|Area|Info|")
                .AppendLine("|---|---|")
                .AppendLine($"|Description|{service.Description}|")
                .AppendLine($"|Service|{service.Name}")
                .AppendLine($"|NuGet|{Utils.ToNugetShield(package.Name, package.Name)}|")
                .AppendLine($"|Static Generated Class|{service.Static}")
                .ToString();


        static string RenderStartup(PackageService service)
        {
            // startup tab (can auto register, service reg),
            var sb = new StringBuilder();
            //new StartupShortcode().Execute(null, "", null, null).ContentProvider
            sb.AppendLine("<?! Startup ?>");

            var reg = $"services.{service.Startup}";
            if (service.BgDelegate != null)
            {
                sb.AppendLine($"{reg}<{service.BgDelegate}>({service.StartupArgs});");

                if (!service.BgDelegateRequired)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{reg}({service.StartupArgs});");
                }
            }
            sb.Append($"services.{service.Startup}");

            sb.AppendLine("<?!/ Startup ?>");
            return sb.ToString();
        }


        static string RenderIosTab(iOSConfig ios)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Minimum Version: " + ios.MinVersion);

            RenderAppDelegate(ios, sb);
            RenderInfoPlist(ios, sb);
            RenderEntitlementsPlist(ios, sb);
            return sb.ToString();
        }


        static void RenderAppDelegate(iOSConfig ios, StringBuilder sb)
        {
            if (!ios.UsesPush && !ios.UsesJobs && !ios.UsesBgTransfers)
                return;

            sb
                .AppendLine("### AppDelegate")
                .AppendLine()
                .AppendLine("```csharp")
                .AppendLine("using System;")
                .AppendLine("using Foundation;")
                .AppendLine("using Xamarin.Forms.Platform.iOS;")
                .AppendLine("using Shiny;")
                .AppendLine()
                .AppendLine("namespace YourIosApp")
                .AppendLine("{")
                .AppendLine("   [Register(\"AppDelegate\")]")
                .AppendLine("   public partial class AppDelegate : FormsApplicationDelegate")
                .AppendLine("   {")
                .AppendLine("       public override bool FinishedLaunching(UIApplication app, NSDictionary options)")
                .AppendLine("       {")
                .AppendLine("           this.ShinyFinishedLaunching(new Samples.SampleStartup());")
                .AppendLine("           global::Xamarin.Forms.Forms.Init();")
                .AppendLine("           this.LoadApplication(new Samples.App());")
                .AppendLine("       }");

            if (ios.UsesJobs)
                sb.AppendLine("public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyPerformFetch(completionHandler);");

            if (ios.UsesPush)
            {
                sb
                    .AppendLine("public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) => this.ShinyRegisteredForRemoteNotifications(deviceToken);")
                    .AppendLine("public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) => this.ShinyFailedToRegisterForRemoteNotifications(error);")
                    .AppendLine("public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);");
            }

            if (ios.UsesBgTransfers)
                sb.AppendLine("public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);");

            sb
                .AppendLine("\t}")
                .AppendLine("}")
                .AppendLine("```");
        }


        static void RenderInfoPlist(iOSConfig ios, StringBuilder sb)
        {
            if (!ios.UsesBgTransfers && !ios.UsesJobs && !ios.UsesPush && ios.InfoPlistValues == null && ios.BackgroundModes == null)
                return;

            sb
                .AppendXmlCode("Info.plist")
                .AppendLine("<!DOCTYPE plist PUBLIC \" -//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">")
                .AppendLine("<plist version=\"1.0\">")
                .AppendLine("<dict>");

            if (ios.UsesJobs)
            {
                sb.Append(@"
    <key>BGTaskSchedulerPermittedIdentifiers</key>
    <array>
        <string>com.shiny.job</string>
        <string>com.shiny.jobpower</string>
        <string>com.shiny.jobnet</string>
        <string>com.shiny.jobpowernet</string>
    </array>
");
            }

            if (ios.InfoPlistValues != null)
            {
                foreach (var value in ios.InfoPlistValues)
                {
                    sb
                        .AppendLine($"    <key>{value}</key>")
                        .AppendLine("    <string>Say something useful here that your users will understand</string>");
                }
            }

            if (ios.BackgroundModes != null || ios.UsesJobs || ios.UsesPush)
            {
                sb
                    .AppendLine("    <key>UIBackgroundModes</key>")
                    .AppendLine("    <array>");

                if (ios.UsesJobs)
                {
                    sb
                        .AppendLine("       <string>processing</string>")
                        .AppendLine("       <string>fetch</string>");
                }
                if (ios.UsesPush)
                {
                    sb.AppendLine("     <string>remote-notification</string>");
                }
                if (ios.BackgroundModes != null)
                {
                    foreach (var bg in ios.BackgroundModes)
                    {
                        sb.AppendLine($"        <string>{bg}</string>");
                    }
                }
                sb.AppendLine("    </array>");
            }
            sb
                .AppendLine("</dict>")
                .AppendLine("</plist>")
                .AppendLine("```");
        }


        static void RenderEntitlementsPlist(iOSConfig ios, StringBuilder sb)
        {
            if (ios.EntitlementPlistValues == null && !ios.UsesPush)
                return;

            sb
                .AppendXmlCode("Entitlements.plist")
                .AppendLine("<!DOCTYPE plist PUBLIC \" -//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">")
                .AppendLine("<plist version=\"1.0\">")
                .AppendLine("<dict>");

            if (ios.EntitlementPlistValues != null)
            {
                foreach (var pair in ios.EntitlementPlistValues)
                {
                    sb.AppendLine($"    <key>{pair.Key}</key>");
                    if (pair.Value.StartsWith("<"))
                        sb.AppendLine($"    {pair.Value}");
                    else
                        sb.AppendLine($"    <string>{pair.Value}</string>");
                }
            }

            if (ios.UsesPush)
            {
                sb
                    .AppendLine("   <key>aps-environment</key>")
                    .AppendLine("   <string>development OR production</string>");
            }
            sb
                .AppendLine("</dict>")
                .AppendLine("</plist>")
                .AppendLine("```");
        }

        static string RenderAndroid(AndroidConfig android)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Minimum Version: " + android.MinVersion);

            if (android.ManifestUsesFeatures != null || android.ManifestUsesPermissions != null)
            {
                sb
                    .AppendXmlCode("AndroidManifest.xml")
                    .AppendLine("<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" android:versionCode=\"1\" android:versionName=\"1.0\" package=\"com.org.yourapp\" android:installLocation=\"preferExternal\">");

                if (android.ManifestUsesPermissions != null)
                    foreach (var permission in android.ManifestUsesPermissions)
                        sb.AppendLine($"    <uses-permission android:name=\"{permission}\" />");

                if (android.ManifestUsesFeatures != null)
                    foreach (var feature in android.ManifestUsesFeatures)
                        sb.AppendLine($"    <uses-feature android:name=\"{feature}\" />");

                sb
                    .AppendLine("</manifest>")
                    .AppendLine("```");

            }
            return sb.ToString();
        }


        static string RenderUwp(UwpConfig uwp)
        {
            if (uwp.DeviceCapabilities == null && uwp.Capabilities == null && uwp.BackgroundTasks == null)
                return "No Special Configuration Required";

            var sb = new StringBuilder()
                .AppendXmlCode("Package.appxmanifest")
                .AppendLine("<Package>")
                .AppendLine("    <Applications>");

            if (uwp.BackgroundTasks != null)
            {
                sb.AppendLine("        <Extensions>");
                foreach (var task in uwp.BackgroundTasks)
                    sb.Append($"            <Task Type=\"{task}\" />");

                sb.AppendLine("        </Extensions>");
            }

            sb.AppendLine("    </Applications>");

            if (uwp.DeviceCapabilities != null || uwp.Capabilities != null)
            {
                sb.AppendLine("    <Capabilities>");
                if (uwp.Capabilities != null)
                    foreach (var cap in uwp.Capabilities)
                        sb.AppendLine($"        <Capability Name=\"{cap}\" />");

                if (uwp.DeviceCapabilities != null)
                    foreach (var dc in uwp.DeviceCapabilities)
                        sb.AppendLine($"        <DeviceCapability Name=\"{dc}\" />");

                sb.AppendLine("    </Capabilities>");
            }

            sb.AppendLine("</Package>");
            return sb.ToString();
        }
    }
}