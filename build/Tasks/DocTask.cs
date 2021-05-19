using System;
using System.Collections.Generic;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Run;
using Cake.Core.Diagnostics;
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
                RunIt(context, "--deploy");
                context.Log.Information("Documentation Deployed");
            }
            else
            {
                context.Log.Information("Building Documentation");
                RunIt(context, null);
                context.Log.Information("Documentation Built");
            }
        }


        static void RunIt(BuildContext context, string? args)
        {
            context.DotNetCoreRestore(Project);

            var settings = new DotNetCoreRunSettings
            {
                //Framework = "net5.0",
                //ArgumentCustomization = args => args.Append("--l Debug"),
                Configuration = context.MsBuildConfiguration,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "GITHUB_TOKEN", context.DocsDeployGitHubToken }
                }
            };
            context.DotNetCoreRun(Project, args, settings);
        }
    }
}
