using System;
using Cake.Common.Diagnostics;
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
            var settings = new DotNetCoreNuGetPushSettings
            {
                ApiKey = context.NugetApiKey,
                Source = MainNuget,
                SkipDuplicate = true
            };

            var packages = context.GetFiles("src/**/*.nupkg");
            foreach (var package in packages)
            {
                try
                {
                    context.DotNetCoreNuGetPush(package.FullPath, settings);
                }
                catch (Exception ex)
                {
                    if (context.AllowNugetUploadFailures)
                        context.Error($"Error Upload: {package.FullPath} - Exception: {ex}");
                    else
                        throw; // break build
                }
            }
        }
    }
}
