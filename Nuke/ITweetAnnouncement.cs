using System;

using Nuke.Common;

using Tweetinvi;

interface ITweetAnnouncement : ITwitterCredentials, IAnnounceMessage
{
    Target Tweet => _ => _
        .TriggeredBy<INugetPublish>()
        .Executes(async () =>
        {
            var client = new TwitterClient(
                this.ConsumerKey,
                this.ConsumerSecret,
                this.AccessToken,
                this.AccessTokenSecret
            );

            await client.Tweets.PublishTweetAsync(this.Message);
        });
}