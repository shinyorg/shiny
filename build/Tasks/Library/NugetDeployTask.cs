using System;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Library
{
    [TaskName("NugetDeploy")]
    [IsDependentOn(typeof(BuildTask))]
    //[IsDependeeOf(typeof(BasicTestsTask))]
    public sealed class NugetDeployTask : FrostingTask<BuildContext>
    {
        const string MainNuget = "https://api.nuget.org/v3/index.json";

        public override bool ShouldRun(BuildContext context)
        {
            var result = context.IsNugetDeployBranch && context.IsRunningInCI;
            if (result && String.IsNullOrWhiteSpace(context.NugetApiKey))
                throw new ArgumentException("NugetApiKey is missing");

            return result;
        }


        public override void Run(BuildContext context)
        {
            // delete symbols for now
            context.DeleteFiles("src/**/*.symbols.nupkg");

            DoDeploy(context, context.NugetApiKey, MainNuget);
        }


        static void DoDeploy(BuildContext context, string apiKey, string sourceUrl)
        {
            var settings = new DotNetCoreNuGetPushSettings
            {
                ApiKey = apiKey,
                Source = sourceUrl,
                SkipDuplicate = true
            };
            // delete symbols for now
            context.DeleteFiles("src/**/*.symbols.nupkg");

            var packages = context.GetFiles("src/**/*.nupkg");
            foreach (var package in packages)
                context.DotNetCoreNuGetPush(package.FullPath, settings);
        }
    }
}
