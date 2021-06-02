using System;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;


namespace ShinyBuild.Tasks.iOS
{
    [TaskName("IosBuild")]
    [IsDependentOn(typeof(InstallCertificateTask))]
    [IsDependentOn(typeof(InstallProvisioningProfileTask))]
    [IsDependentOn(typeof(PListTask))]
    [IsDependentOn(typeof(BootsTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI && context.IsRunningOnMacOs();


        public override void Run(BuildContext context)
        {
            context.CleanDirectory($"../samples/**/bin/{context.MsBuildConfiguration}");
            context.MSBuild("Build.sln", x => x
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("Platform", "iPhone")
                .WithProperty("BuildIpa", "true")
            );
        }
    }
}