using System;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Library
{
    [TaskName("Build")]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        // needs to be windows build for UWP
        public override bool ShouldRun(BuildContext context)
        {
            if (!context.IsWindows)
                return false;

            if (context.IsRunningInCI && context.BuildNumber == 0)
                throw new ArgumentException("BuildNumber argument is missing");

            return true;
        }


        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"./src/**/obj/");
            context.CleanDirectories($"./src/**/bin/{context.MsBuildConfiguration}");

            var version = GetNugetVersion(context);

            context.MSBuild("Build.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("ShinyVersion", version)
                .WithProperty("CI", context.IsRunningInCI ? "true" : "")
                .WithProperty("OS", context.OperatingSystemString)
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }


        static string GetNugetVersion(BuildContext context)
        {
            var version = $"{context.MajorMinorVersion}.{context.BuildNumber}";
            if (!context.IsMainBranch)
                version += "-preview";

            context.Log.Information("Shiny Version: " + version);
            return version;
        }
    }
}