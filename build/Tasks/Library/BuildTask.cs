using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Library
{
    [TaskName("Build")]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        // needs to be windows build for UWP
        public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();


        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"./src/**/obj/");
            context.CleanDirectories($"./src/**/bin/{context.MsBuildConfiguration}");

            context.MSBuild("Build.slnf", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("CI", context.IsRunningInCI ? "true" : "")
                .WithProperty("OS", context.OperatingSystemString)
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }
    }
}