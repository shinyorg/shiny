using System;
using Cake.Common;
using Cake.Common.Build;
using Cake.Core;
using Cake.Frosting;


namespace ShinyBuild
{
    public class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context) : base(context)
        {
            this.MsBuildConfiguration = context.Argument("configuration", "Release");
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