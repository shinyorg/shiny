using System;
using System.Collections.Generic;
using Cake.AppCenter;
using Cake.Common.IO;
using Cake.Frosting;
using Cake.Plist;


namespace ShinyBuild.Samples.Tasks
{
    public sealed class IosPlistTask : FrostingTask<BuildContext>
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


    [TaskName("IosBuild")]
    [IsDependentOn(typeof(IosPlistTask))]
    public sealed class BuildIosTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectory($"../samples/**/bin/{context.MsBuildConfiguration}");
            //MSBuild("./MyDirectory/MyProject.sln", settings =>
            //settings.SetConfiguration("Release")
            //    .WithTarget("Build")
            //    .WithProperty("Platform", "iPhone")
            //    .WithProperty("BuildIpa", "true")
            //    .WithProperty("OutputPath", "bin/Release/")
            //    .WithProperty("TreatWarningsAsErrors", "false"));
            // TODO: mobile.buildtools handles secrets well in the IDE and therefore can also be used here
            // TODO: separate iOS and android for build and deploy
        }
    }


    public sealed class SignIosBuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {

        }
    }


    [IsDependentOn(typeof(BuildIosTask))]
    [IsDependentOn(typeof(SignIosBuildTask))]
    public sealed class IosDeployTask : FrostingTask<BuildContext>
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