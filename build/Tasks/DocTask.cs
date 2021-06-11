using System;
using System.Collections.Generic;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Run;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    [TaskName("Documentation")]
    public sealed class DocTask : FrostingTask<BuildContext>
    {
        const string Project = "docs/docs.csproj";


        public override void Run(BuildContext context)
        {
            if (context.IsRunningInCI && context.IsDocsDeployBranch)
            {
                if (String.IsNullOrWhiteSpace(context.DocsDeployGitHubToken))
                    throw new ArgumentException("Docs GitHub Deployment Token is missing");

                context.Log.Information("Building & Deploying Documentation");
                RunIt(context, true);
                context.Log.Information("Documentation Deployed");
            }
            else
            {
                context.Log.Information("Building Documentation");
                RunIt(context, false);
                context.Log.Information("Documentation Built");
            }
        }


        static void RunIt(BuildContext context, bool deploy)
        {
            context.DotNetCoreRestore(Project);

            var settings = new DotNetCoreRunSettings
            {
                Configuration = context.MsBuildConfiguration,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "GITHUB_TOKEN", context.DocsDeployGitHubToken }
                }
            };
            var args = new ProcessArgumentBuilder();
            if (deploy)
                args.Append("deploy");

            context.DotNetCoreRun(Project, args, settings);
        }
    }
}
