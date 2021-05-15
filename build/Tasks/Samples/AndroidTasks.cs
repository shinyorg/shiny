using System;
using Cake.AndroidAppManifest;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;


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


    [TaskName("AndroidBuild")]
    public sealed class SampleBuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.MSBuild("Samples.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("OS", context.OperatingSystemString)
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }
    }

    // TODO: sign and deploy tasks

    //[IsDependentOn(typeof(SampleBuildTask))]
    //public sealed class UwpDeployTask : FrostingTask<BuildContext>
    //{
    //    public override void Run(BuildContext context)
    //    {
    //        if (!context.IsRunningInCI)
    //        {
    //            context.Log.Information("Not Deploying to AppCenter");
    //            return;
    //        }
    //        // TODO: deploy Android, iOS, & UWP
    //        context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
    //        {
    //        });
    //        context.Log.Information("AppCenter Deployment Successful");
    //    }
    //}
}