using System;
using Nuke.Common;
using static Nuke.Common.Tools.DotNet.DotNetTasks;


interface IDocumentation : INukeBuild
{
    //[Parameter] [Secret] readonly string PublicNuGetApiKey;
    //[Parameter] [Secret] readonly string GitHubRegistryApiKey;


    Target Build => _ => _.Executes(() =>
    {
    });
    //        const string Project = "docs/docs.csproj";


    //        public override void Run(BuildContext context)
    //        {
    //            if (context.IsRunningInCI && context.IsDocsDeployBranch)
    //            {
    //                if (String.IsNullOrWhiteSpace(context.DocsDeployGitHubToken))
    //                    throw new ArgumentException("Docs GitHub Deployment Token is missing");

    //                context.Log.Information("Building & Deploying Documentation");
    //                RunIt(context, true);
    //                context.Log.Information("Documentation Deployed");
    //            }
    //            else
    //            {
    //                context.Log.Information("Building Documentation");
    //                RunIt(context, false);
    //                context.Log.Information("Documentation Built");
    //            }
    //        }


    //        static void RunIt(BuildContext context, bool deploy)
    //        {
    //            context.DotNetCoreRestore(Project);

    //            var settings = new DotNetCoreRunSettings
    //            {
    //                Configuration = context.MsBuildConfiguration,
    //                EnvironmentVariables = new Dictionary<string, string>
    //                {
    //                    { "GITHUB_TOKEN", context.DocsDeployGitHubToken }
    //                }
    //            };
    //            var args = new ProcessArgumentBuilder();
    //            if (deploy)
    //                args.Append("deploy");

    //            context.DotNetCoreRun(Project, args, settings);
    //        }
}
