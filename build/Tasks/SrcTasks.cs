using System;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.MSBuild;
using Cake.Core.IO;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    [TaskName("Clean")]
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"./src/**/bin/{context.MsBuildConfiguration}");
        }
    }


    [TaskName("Build")]
    [IsDependentOn(typeof(CleanTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var versionInfo = context.GitVersion(new GitVersionSettings
            {
                UpdateAssemblyInfo = false,
                OutputType = GitVersionOutput.Json,
                LogFilePath = context.GitVersionLog.MakeAbsolute(context.Environment)
            });
            var version = versionInfo.InformationalVersion;
            if (!context.IsMainBranch)
                version += "-preview";

            var os = context.Environment.Platform.Family == Cake.Core.PlatformFamily.Windows ? "WINDOWS_NT" : "MAC";

            context.MSBuild("Build.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("ShinyVersion", version)
                .WithProperty("OS", os)
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
            if (!context.Branch.FriendlyName.Equals("preview") && !context.IsMainBranch)
                return;

            var settings = new DotNetCoreNuGetPushSettings
            {
                ApiKey = context.NugetApiKey,
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true
            };
            var packages = context.GetFiles("../src/**/*.nupkg");
            foreach (var package in packages)
                context.DotNetCoreNuGetPush(package.FullPath, settings);
        }
    }


    [TaskName("Default")]
    [IsDependentOn(typeof(NugetDeployTask))]
    [IsDependentOn(typeof(DocTask))]
    public sealed class DefaultTask : FrostingTask<BuildContext> { }
}