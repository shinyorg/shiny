using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Cake.AndroidAppManifest;
using Cake.AppCenter;
using Cake.Boots;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Plist;


namespace ShinyBuild.Tasks
{
    [TaskName("Sample-AndroidManifest")]
    public sealed class SampleAndroidManifestTask : FrostingTask<BuildContext>
    {
        const string DroidManifest = "samples/Samples.Android/Properties/AndroidManifest.xml";
        public override void Run(BuildContext context)
        {
            var manifest = context.DeserializeAppManifest(DroidManifest);
            manifest.VersionName = "1.0";
            manifest.VersionCode = 1;
            manifest.PackageName = "com.shinyorg.samples";
            context.SerializeAppManifest(DroidManifest, manifest);
        }
    }


    [TaskName("Sample-iOSPlists")]
    public sealed class SampleIosPlistTask : FrostingTask<BuildContext>
    {
        const string InfoPlist = "samples/Samples.iOS/Info.plist";
        const string EntitlementsPlist = "samples/Samples.iOS/Entitlements.plist";

        public override void Run(BuildContext context)
        {
            var infoPath = context.File(InfoPlist);

            var info = (Dictionary<string, object>)context.DeserializePlist(infoPath);
            info["CFBundleDisplayName"] = "Shiny Sample";
            info["CFBundleIdentifier"] = "com.shiny.test";

            var version = Version.Parse((string)info["CFBundleVersion"]);
            var newVersion = new Version(version.Major, version.Minor, version.Build + 1, 0);
            info["CFBundleVersion"] = newVersion.ToString();
            context.SerializePlist(infoPath, info);

            var entitlementPath = context.File(EntitlementsPlist);
            var entitlements = context.DeserializePlist(entitlementPath);
            entitlements["aps-environment"] = "development"; // production
            context.SerializePlist(entitlementPath, info);
        }
    }


    [TaskName("Sample-Clean")]
    public sealed class SampleCleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"samples/**/bin/{context.MsBuildConfiguration}");
        }
    }


    [TaskName("Sample-Build")]
    [IsDependentOn(typeof(SampleAndroidManifestTask))]
    [IsDependentOn(typeof(SampleIosPlistTask))]
    [IsDependentOn(typeof(SampleCleanTask))]
    public sealed class SampleBuildTask : AsyncFrostingTask<BuildContext>
    {
        const ReleaseChannel Channel = ReleaseChannel.Stable;


        public override async Task RunAsync(BuildContext context)
        {
            if (context.IsRunningInCI)
            {
                context.Log.Information($"Installing Boots - {Channel} Channel");

                await context.Boots(Product.Mono, Channel);
                await context.Boots(Product.XamarinAndroid, Channel);
                await context.Boots(Product.XamariniOS, Channel);

                context.Log.Information($"Boots {Channel} Installed Successfully");
            }
            context.MSBuild("Samples.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("OS", context.OperatingSystemString)
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }
    }


    [TaskName("Sample-Deploy")]
    [IsDependentOn(typeof(SampleBuildTask))]
    public sealed class SampleDeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (!context.IsRunningInCI)
            {
                context.Log.Information("Not Deploying to AppCenter");
                return;
            }
            // TODO: deploy Android, iOS, & UWP
            context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
            {
            });
            context.Log.Information("AppCenter Deployment Successful");
        }
    }
}