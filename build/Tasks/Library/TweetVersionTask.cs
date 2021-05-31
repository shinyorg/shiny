using System;
using System.Threading.Tasks;

using Cake.Core.Diagnostics;
using Cake.Frosting;
using Tweetinvi;


namespace ShinyBuild.Tasks
{
    public class TweetVersionTask : AsyncFrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context) => context.IsMainBranch && context.IsRunningInCI;


        public override async Task RunAsync(BuildContext context)
        {
            try
            {
                var client = new TwitterClient("CONSUMER_KEY", "CONSUMER_SECRET", "ACCESS_TOKEN", "ACCESS_TOKEN_SECRET");
                await client.Tweets.PublishTweetAsync($"Shiny v{context.NugetVersion} released! Check out the latest release notes here - https://shinylib.net/release-notes/");
            }
            catch (Exception ex)
            {
                // don't fail the build because of a failed tweet
                context.Log.Error($"Error publishing release Tweet - {ex}");
            }
        }
    }
}
