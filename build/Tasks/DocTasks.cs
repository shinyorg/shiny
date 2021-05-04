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
            if (!context.IsRunningInCI)
                RunIt(context, null);

            else if (context.IsMainBranch)
                RunIt(context, "--deploy");
        }


        static void RunIt(BuildContext context, string? args)
        {
            context.DotNetCoreRestore(Project);
            context.DotNetCoreRun(Project, args, Settings);
        }
    }
}
