using System;
using Cake.AndroidAppManifest;
using Cake.AppCenter;
using Cake.Common.IO;
using Cake.Frosting;
using Cake.Plist;


namespace ShinyBuild.Tasks
{
    [TaskName("Sample-AndroidManifest")]
    public sealed class SampleAndroidManifestTask : FrostingTask<BuildContext>
    {
        const string DroidManifest = "../samples/Samples.Android/Properties/AndroidManifest.xml";
        public override void Run(BuildContext context)
        {
            var manifest = context.DeserializeAppManifest(DroidManifest);
            manifest.VersionName = "";
            manifest.VersionCode = 1;
            manifest.PackageName = "";
            context.SerializeAppManifest(DroidManifest, manifest);
        }
    }


    [TaskName("Sample-iOSPlists")]
    public sealed class SampleIosPlistTask : FrostingTask<BuildContext>
    {
        const string InfoPlist = "../samples/Samples.iOS/Info.plist";
        const string EntitlementsPlist = "../samples/Samples.iOS/Entitlements.plist";

        public override void Run(BuildContext context)
        {
            var infoPath = context.File(InfoPlist);

            var info = context.DeserializePlist(infoPath);
            info.CFBundleDisplayName = "Shiny Sample";
            info.CFBundleIdentifier = "com.shiny.test";

            Version version = Version.Parse(info.CFBundleVersion);
            var newVersion = new Version(version.Major, version.Minor, version.Build + 1, 0);
            info.CFBundleVersion = newVersion.ToString();
            context.SerializePlist(infoPath, (object)info);

            var entitlementPath = context.File(EntitlementsPlist);
            var entitlements = context.DeserializePlist(entitlementPath);
            entitlements["aps-environment"] = "development"; // production
            context.SerializePlist(entitlementPath, (object)info);
        }
    }


    [TaskName("Sample-Clean")]
    public sealed class SampleCleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectory($"../samples/**/bin/{context.MsBuildConfiguration}");
        }
    }


    [TaskName("Sample-Build")]
    [IsDependentOn(typeof(BootsTask))]
    [IsDependentOn(typeof(SampleAndroidManifestTask))]
    [IsDependentOn(typeof(SampleIosPlistTask))]
    [IsDependentOn(typeof(SampleCleanTask))]
    public sealed class SampleBuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            // TODO: mobile.buildtools handles secrets well in the IDE and therefore can also be used here
            // TODO: separate iOS and android for build and deploy
        }
    }


    [TaskName("Sample-Deploy")]
    [IsDependentOn(typeof(SampleBuildTask))]
    public sealed class SampleDeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (!context.IsMainBranch || !context.IsRunningInCI)
                return;

            // TODO: deploy Android, iOS, & UWP
            context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
            {
            });
        }
    }
}