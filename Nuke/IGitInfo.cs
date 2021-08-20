using Nuke.Common;
using Nuke.Common.Git;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;


interface IGitInfo
{
    [Parameter] [Secret] string GitHubToken => TryGetValue(() => GitHubToken);
    [GitRepository] [Required] GitRepository GitRepository => TryGetValue(() => GitRepository);
}