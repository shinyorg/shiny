using System;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Run;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    [TaskName("Documentation")]
    public sealed class DocTask : FrostingTask<BuildContext>
    {
        const string Project = "../docs/docs.csproj";
        static readonly DotNetCoreRunSettings Settings = new DotNetCoreRunSettings
        {
            Framework = "net5.0",
            Configuration = "Release"
        };


        public override void Run(BuildContext context)
        {
            if (context.IsRunningInCI && context.IsMainBranch)
                RunIt(context, "--deploy");
            else
                RunIt(context, null);
        }


        static void RunIt(BuildContext context, string? args)
        {
            context.DotNetCoreRestore(Project);
            context.DotNetCoreRun(Project, args, Settings);
        }
    }
}
