using System.Threading.Tasks;

namespace Shiny.Push;


public static class PushExtensions
{
    public static bool IsTagsSupport(this IPushManager push)
        => push.Tags != null;

    public static async Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, params string[] tags)
    {
        var access = await pushManager
            .RequestAccess()
            .ConfigureAwait(false);

        if (pushManager.Tags != null)
        {
            foreach (var tag in tags)
            {
                await pushManager
                    .Tags
                    .AddTag(tag)
                    .ConfigureAwait(false);
            }
        }
        return access;
    }
}