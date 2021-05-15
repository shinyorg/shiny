using System;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;


namespace ShinyBuild
{
    public class BuildLifetime : IFrostingLifetime
    {
        public void Setup(ICakeContext context)
        {
            var build = new BuildContext(context);
            if (!build.IsRunningInCI)
            {
                build.Log.Information("Not Running In CI");
                return;
            }

            var error = false;
            if (build.BuildNumber == 0)
            {
                build.Log.Error("BuildNumber argument is missing");
                error = true;
            }
            if (build.IsNugetDeployBranch && String.IsNullOrWhiteSpace(build.NugetApiKey))
            {
                build.Log.Error("NuGet API Key is missing");
                error = true;
            }
            if (build.IsDocsDeployBranch && String.IsNullOrWhiteSpace(build.DocsDeployGitHubToken))
            {
                build.Log.Error("Docs GitHub Deployment Token is missing");
                error = true;
            }
            if (error)
                throw new ArgumentException("Missing Build Arguments");
        }


        public void Teardown(ICakeContext context, ITeardownContext info)
        {
        }
    }
}
