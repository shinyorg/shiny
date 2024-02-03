using System.Threading.Tasks;

namespace Shiny.Push;


public static class PushExtensions
{
    public static bool IsTagsSupport(this IPushManager push)
        => push.Tags != null;


    public static async Task<bool> TrySetTags(this IPushManager pushManager, params string[] tags)
    {
        if (pushManager.Tags == null)
            return false;

        await pushManager
            .Tags
            .SetTags(tags)
            .ConfigureAwait(false);

        return true;
    }


    public static string[]? TryGetTags(this IPushManager pushManager)
        => pushManager.Tags?.RegisteredTags;


    public static async Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, params string[] tags)
    {
        var access = await pushManager
            .RequestAccess()
            .ConfigureAwait(false);

        if (pushManager.Tags != null)
        {
            await pushManager
                .Tags
                .SetTags(tags)
                .ConfigureAwait(false);
        }
        return access;
    }
}