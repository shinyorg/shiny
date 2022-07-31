//using Cake.Frosting;
//using Cake.GitVersioning;
//using Octokit;

//namespace ShinyBuild.Tasks.Library;


//[IsDependentOn(typeof(NugetDeployTask))]
//public class GitHubReleaseTask : AsyncFrostingTask<BuildContext>
//{
//    public override bool ShouldRun(BuildContext context) => context.IsReleaseBranch && context.IsRunningInCI;


//    public override async Task RunAsync(BuildContext context)
//    {
//        var client = new GitHubClient(new ProductHeaderValue("ShinyRelease"))
//        {
//            Credentials = new Credentials(context.GitHubSecretToken)
//        };

//        //https://octokitnet.readthedocs.io/en/latest/releases/#upload-assets
//        var release = new NewRelease("v" + context.ReleaseVersion);
//        release.Name = "v" + context.ReleaseVersion;
//        release.Body = this.GetReleaseNotes(context);
//        release.Draft = false;
//        release.Prerelease = false;
//        var result = await client.Repository.Release.Create("shinyorg", "shiny", release);
//    }


//    string GetReleaseNotes(BuildContext context)
//    {
//        var v = context.GitVersioningGetVersion();
//        var path = $"./docs/Input/release-notes/v{v.VersionMajor}.{v.VersionMinor}.{v.VersionRevision}.md";
//        if (!File.Exists(path))
//        {
//            // TODO: exception?
//        }
//        return File.ReadAllText(path);
//    }
//}
