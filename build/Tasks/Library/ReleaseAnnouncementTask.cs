using System;
using System.Threading.Tasks;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Discord;
using Discord.WebSocket;
using Tweetinvi;


namespace ShinyBuild.Tasks
{
    public class ReleaseAnnouncementTask : AsyncFrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsNugetDeployBranch && context.IsRunningInCI;
        //=> context.IsMainBranch && context.IsRunningInCI;


        public override Task RunAsync(BuildContext context)
        {
            var message = $"Shiny v{context.NugetVersion} released! Check out the latest release notes here - https://shinylib.net/release-notes/";
            return Task.WhenAll(
                this.Twitter(context, message),
                this.Discord(context, message)
            );
        }


        async Task Twitter(BuildContext context, string message)
        {
            try
            {
                var consumerKey = context.ArgumentOrEnvironment<string>("TwitterConsumerKey");
                var consumerSecret = context.ArgumentOrEnvironment<string>("TwitterConsumerSecret");
                var accessToken = context.ArgumentOrEnvironment<string>("TwitterAccessToken");
                var accessTokenSecret = context.ArgumentOrEnvironment<string>("TwitterAccessTokenSecret");

                var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                await client.Tweets.PublishTweetAsync(message);
            }
            catch (Exception ex)
            {
                // don't fail the build because of a failed tweet
                context.Log.Error($"Error publishing release Tweet - {ex}");
            }
        }


        async Task Discord(BuildContext context, string message)
        {
            try
            {
                var discordToken = context.ArgumentOrEnvironment<string>("DiscordToken");
                var discordChannel = context.ArgumentOrEnvironment<ulong>("DiscordChannelId"); // 803717285986566174 - #shinylib on sponsorconnect

                var client = new DiscordSocketClient();
                await client.LoginAsync(TokenType.Bot, discordToken, true);
                var channel = client.GetChannel(discordChannel) as IMessageChannel;
                await channel.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                context.Log.Error($"Error publishing release message to Discord - {ex}");
            }
        }
    }
}
