using System;
using Cake.Common;
using Cake.Common.Build;
using Cake.Core;
using Cake.Frosting;
using Cake.Git;


namespace ShinyBuild
{
    public class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context) : base(context)
        {
            this.MsBuildConfiguration = context.Argument("configuration", "Release");
            //context.Environment.WorkingDirectory
            this.Branch = context.GitBranchCurrent(".");
        }


        public string NugetApiKey { get; } = "";

        public GitBranch Branch { get; }
        public bool IsMainBranch
        {
            get
            {
                var bn = this.Branch.FriendlyName.ToLower();
                return bn.Equals("main") || bn.Equals("master");
            }
        }


        public bool IsRunningInCI
        {
            get
            {
                var ga = this.GitHubActions();
                if (!ga.IsRunningOnGitHubActions)
                    return false;

                //if (ga.Environment.PullRequest.IsPullRequest)
                return true;
            }
        }


        public string MsBuildConfiguration { get; set; }
    }
}