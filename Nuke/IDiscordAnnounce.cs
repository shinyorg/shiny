using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Nuke.Common;


interface IDiscordAnnounce : IDiscordCredentials, IAnnounceMessage
{
    Target Send => _ => _.Executes(async () =>
    {
        var guildId = ulong.Parse(this.GuildId);
        var channelId = ulong.Parse(this.ChannelId);

        var client = new DiscordSocketClient();
        await client.LoginAsync(TokenType.Bot, this.Token, true);
        await client.StartAsync();

        SocketGuild? guild = null;
        var count = 0;

        while (guild == null)
        {
            guild = client.GetGuild(guildId);
            await Task.Delay(2000);
            count++;
            if (count == 5)
                throw new ArgumentException("Could not retrieve guild");
        }

        await guild
            .TextChannels
            .First(x => x.Id == channelId)
            .SendMessageAsync(this.Message);
    });
}