//using System.Threading.Tasks;
//using Cake.Frosting;
//using Octokit;


//namespace ShinyBuild.Tasks.Library
//{
//    [IsDependentOn(typeof(NugetDeployTask))]
//    public class GitHubReleaseTask : AsyncFrostingTask<BuildContext>
//    {
//        public override async Task RunAsync(BuildContext context)
//        {
//            var client = new GitHubClient(new ProductHeaderValue("ShinyRelease"))
//            {
//                Credentials = new Credentials("GITHUB_TOKEN")
//            };
//            //https://octokitnet.readthedocs.io/en/latest/releases/#upload-assets
//            var release = new NewRelease("v" + context.NugetVersion);
//            release.Name = "v" + context.NugetVersion;
//            //release.Body = ""; // TODO: suck in contents of docs/Input/release-notes/latest
//            release.Draft = false;
//            release.Prerelease = false;
//            var result = await client.Repository.Release.Create("shinyorg", "shiny", release);
//        }
//    }
//}
