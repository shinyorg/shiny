using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;
using Cake.Git;


namespace ShinyBuild.Tasks
{
    [TaskName("Clean")]
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectory($"../src/**/bin/{context.MsBuildConfiguration}");
        }
    }


    //[TaskName("Tests")]
    //public sealed class BasicTestsTask : FrostingTask<BuildContext>
    //{
    //    public override void Run(BuildContext context)
    //    {
    //        context.XUnit2("../tests/");
    //    }
    //}


    [TaskName("Build")]
    [IsDependentOn(typeof(CleanTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var branch = context.GitBranchCurrent("../");
            // TODO: preview/dev = preview version
            // TODO: main/master = stable
            //if (branch.FriendlyName != "main")
            //    return;

            // TODO: calculate build # using tags if running in github action/CI

            context.MSBuild("../Build.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("BUILD_BUILD_NUMBER", "0")
                .WithProperty("BUILD_SOURCEBRANCHNAME", "ref/branch/main")
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }
    }


    [TaskName("NugetDeploy")]
    [IsDependentOn(typeof(BuildTask))]
    //[IsDependeeOf(typeof(BasicTestsTask))]
    public sealed class NugetDeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var branch = context.GitBranchCurrent("../");
            if (branch.FriendlyName != "main")
                return;

            if (!context.IsRunningInCI)
                return;

            // myget?
            var settings = new DotNetCoreNuGetPushSettings
            {
                ApiKey = "",
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true
            };
            var packages = context.GetFiles("../src/**/*.nupkg");
            foreach (var package in packages)
            {
                context.DotNetCoreNuGetPush(package.FullPath, settings);
            }
        }
    }
}