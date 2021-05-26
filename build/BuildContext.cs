using System;
using System.IO;
using Cake.Common;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Git;


namespace ShinyBuild
{
    public class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context) : base(context)
        {
#if DEBUG
            //walk backwards until git directory found -that's root
            if (!context.GitIsValidRepository(context.Environment.WorkingDirectory))
            {
                var dir = new DirectoryPath(".");
                while (!context.GitIsValidRepository(dir))
                    dir = new DirectoryPath(Directory.GetParent(dir.FullPath).FullName);

                context.Environment.WorkingDirectory = dir;
            }
            //context.GitHubActions().Environment.Workflow.
#endif
            this.Branch = context.GitBranchCurrent(".");
        }


        public string MajorMinorVersion => "2.0";
        public int BuildNumber => this.Argument("BuildNumber", 0);
        public bool UseXamarinPreview => this.HasArgument("UseXamarinPreview");
        public bool IsWindows => this.Environment.Platform.Family == PlatformFamily.Windows;
        public string DocsDeployGitHubToken => this.Argument<string>(nameof(DocsDeployGitHubToken), null);
        public string OperatingSystemString => this.Environment.Platform.Family == PlatformFamily.Windows ? "WINDOWS_NT" : "MAC";
        public string MsBuildConfiguration => this.Argument("configuration", "Release");
        public string NugetApiKey => this.Argument<string>("NugetApiKey");
        public string ArtifactDirectory => this.GitHubActions().Environment.Workflow.Workspace + "/artifacts";
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


        public bool IsNugetDeployBranch
        {
            get
            {
                var bn = this.Branch.FriendlyName.ToLower();
                return bn.Equals("main") || bn.Equals("master") || bn.Equals("preview");
            }
        }


        public bool IsDocsDeployBranch => this.IsNugetDeployBranch;
    }
}