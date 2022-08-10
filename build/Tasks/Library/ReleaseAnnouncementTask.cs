using System;
using System.Linq;
using System.Threading.Tasks;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Discord;
using Discord.WebSocket;
using Tweetinvi;

namespace ShinyBuild.Tasks.Library;


[IsDependentOn(typeof(NugetDeployTask))]
public class ReleaseAnnouncementTask : AsyncFrostingTask<BuildContext>
{
    const string DocLink = "https://shinylib.net/release-notes/mobile/v";

    public override bool ShouldRun(BuildContext context)
        => context.IsRunningInCI && context.IsNugetDeployBranch;


    public override Task RunAsync(BuildContext context)
    {        
        var link = context.IsReleaseBranch ? $"{DocLink}{context.ReleaseVersion}" : $"{DocLink}next";
        var message = $"Shiny {context.ReleaseVersion} released! Check out the latest release notes here - {link}";
        //var releaseType = context.IsMainBranch ? "" : " **PREVIEW**";
        //var message = $"New Shiny Release{releaseType} - Check out the latest release notes here - https://shinylib.net/release-notes/";
        return Task.WhenAll(
            this.Twitter(context, message),
            this.Discord(context, message)
        );
    }


    async Task Twitter(BuildContext context, string message)
    {
        try
        {
            context.Log.Information("Attempting to publish to Twitter");

            var consumerKey = context.ArgumentOrEnvironment<string>("TwitterConsumerKey");
            var consumerSecret = context.ArgumentOrEnvironment<string>("TwitterConsumerSecret");
            var accessToken = context.ArgumentOrEnvironment<string>("TwitterAccessToken");
            var accessTokenSecret = context.ArgumentOrEnvironment<string>("TwitterAccessTokenSecret");
            var client = new TwitterClient(
                consumerKey,
                consumerSecret,
                accessToken,
                accessTokenSecret
            );

            await client.Tweets.PublishTweetAsync(message);

            context.Log.Information("Tweet Published - " + message);
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
            context.Log.Information("Attempting to publish Discord message");

            var discordToken = context.ArgumentOrEnvironment<string>("DiscordToken");
            var discordGuildId = context.ArgumentOrEnvironment<ulong>("DiscordGuildId");
            var discordChannel = context.ArgumentOrEnvironment<ulong>("DiscordChannelId"); // 803717285986566174 - #shinylib on sponsorconnect

            var client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, discordToken, true);
            await client.StartAsync();
            SocketGuild? guild = null;
            var count = 0;
            while (guild == null)
            {
                guild = client.GetGuild(discordGuildId);
                await Task.Delay(2000);
                count++;
                if (count == 5)
                    throw new ArgumentException("Could not retrieve guild");
            }

            await guild
                .TextChannels
                .First(x => x.Id == discordChannel)
                .SendMessageAsync(message);

            context.Log.Information("Discord Message Published");
        }
        catch (Exception ex)
        {
            context.Log.Error($"Error publishing release message to Discord - {ex}");
        }
    }
}
