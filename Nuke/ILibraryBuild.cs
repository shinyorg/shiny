using System;
using Nuke.Common;


interface ILibraryBuild : INukeBuild, ISolutionAccess
{
    Target Build => _ => _.Executes(() =>
    {
        //MSBuild(x => x);
    });

    //        // needs to be windows build for UWP
    //        public override bool ShouldRun(BuildContext context)
    //        {
    //            if (!context.IsRunningOnWindows())
    //                return false;

    //            if (context.IsRunningInCI && context.BuildNumber == 0)
    //                throw new ArgumentException("BuildNumber argument is missing");

    //            return true;
    //        }


    //        public override void Run(BuildContext context)
    //        {
    //            context.CleanDirectories($"./src/**/obj/");
    //            context.CleanDirectories($"./src/**/bin/{context.MsBuildConfiguration}");

    //            context.MSBuild("Build.sln", x => x
    //                .WithRestore()
    //                .WithTarget("Clean")
    //                .WithTarget("Build")
    //                .WithProperty("ShinyVersion", context.NugetVersion)
    //                .WithProperty("CI", context.IsRunningInCI ? "true" : "")
    //                .WithProperty("OS", context.OperatingSystemString)
    //                .SetConfiguration(context.MsBuildConfiguration)
    //            );
    //        }
}
