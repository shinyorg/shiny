using System;
using System.Threading.Tasks;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Tweetinvi;


namespace ShinyBuild.Tasks
{
    public class TweetVersionTask : AsyncFrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsNugetDeployBranch && context.IsRunningInCI;
        //=> context.IsMainBranch && context.IsRunningInCI;


        public override async Task RunAsync(BuildContext context)
        {
            try
            {
                var consumerKey = context.ArgumentOrEnvironment<string>("TwitterConsumerKey");
                var consumerSecret = context.ArgumentOrEnvironment<string>("TwitterConsumerSecret");
                var accessToken = context.ArgumentOrEnvironment<string>("TwitterAccessToken");
                var accessTokenSecret = context.ArgumentOrEnvironment<string>("TwitterAccessTokenSecret");

                var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
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
