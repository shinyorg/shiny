using Nuke.Common;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;


[ParameterPrefix(Twitter)]
public interface ITwitterCredentials : INukeBuild
{
    public const string Twitter = nameof(Twitter);

    [Parameter] [Secret] string ConsumerKey => TryGetValue(() => ConsumerKey);
    [Parameter] [Secret] string ConsumerSecret => TryGetValue(() => ConsumerSecret);
    [Parameter] [Secret] string AccessToken => TryGetValue(() => AccessToken);
    [Parameter] [Secret] string AccessTokenSecret => TryGetValue(() => AccessTokenSecret);
}