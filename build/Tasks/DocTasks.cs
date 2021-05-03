using System;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Run;
using Cake.Frosting;
using Cake.Git;


namespace ShinyBuild.Tasks
{
    [TaskName("Documentation")]
    public sealed class DocTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var branch = context.GitBranchCurrent("../");
            if (branch.FriendlyName != "main" || !context.IsRunningInCI)
                return;

            context.DotNetCoreRestore("../docs/docs.csproj");
            context.DotNetCoreRun("../docs/docs.csproj", "--deploy", new DotNetCoreRunSettings
            {
                Framework = "net5.0",
                Configuration = "Release"
            });
        }
    }
}
