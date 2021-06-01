using System;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Android
{
    [TaskName("AndroidBuild")]
    [IsDependentOn(typeof(ManifestTask))]
    [IsDependentOn(typeof(BootsTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"samples/**/bin/{context.MsBuildConfiguration}");

            context.MSBuild("Samples.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("OS", context.OperatingSystemString)
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }
    }
}