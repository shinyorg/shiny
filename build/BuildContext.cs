using System;
using System.IO;
using Cake.Common;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.Diagnostics;
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
#endif
            this.Branch = context.GitBranchCurrent(".");

            var version = $"{this.MajorMinorVersion}.{this.BuildNumber}";
            if (!this.IsMainBranch)
                version += "-preview";

            this.Log.Information("Shiny Version: " + version);
            this.NugetVersion = version;
        }


        public string MajorMinorVersion => this.Argument<string>("ShinyVersion", Constants.MajorMinorVersion);
        public int BuildNumber => this.Argument("BuildNumber", 0);
        public bool UseXamarinPreview => this.HasArgument("UseXamarinPreview");
        public bool IsWindows => this.Environment.Platform.Family == PlatformFamily.Windows;
        public string DocsDeployGitHubToken => this.Argument<string>(nameof(DocsDeployGitHubToken), null);
        public string OperatingSystemString => this.Environment.Platform.Family == PlatformFamily.Windows ? "WINDOWS_NT" : "MAC";
        public string MsBuildConfiguration => this.Argument("configuration", Constants.DefaultBuildConfiguration);
        public string NugetApiKey => this.Argument<string>("NugetApiKey");
        public bool AllowNugetUploadFailures => this.HasArgument("AllowNugetUploadFailures");
        public GitBranch Branch { get; }
        public string NugetVersion { get; }


        public string ArtifactDirectory
        {
            get
            {
                if (this.IsRunningInCI)
                    return this.GitHubActions().Environment.Workflow.Workspace + "/artifacts";

                return System.IO.Path.Combine(this.Environment.WorkingDirectory.FullPath, "artifacts");
            }
        }

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