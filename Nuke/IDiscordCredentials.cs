using Nuke.Common;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;


[ParameterPrefix(Discord)]
interface IDiscordCredentials
{
    public const string Discord = nameof(Discord);

    [Parameter] [Secret] string Token => TryGetValue(() => Token);
    [Parameter] [Secret] string GuildId => TryGetValue(() => GuildId); // ulong
    [Parameter] [Secret] string ChannelId => TryGetValue(() => ChannelId); // ulong - // 803717285986566174 - #shinylib on sponsorconnect
}
