using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Convenience extensions over <see cref="IPushManager"/> for tag support detection and combined access/tag operations.
/// </summary>
public static class PushExtensions
{
    /// <summary>
    /// Returns true if the current push provider supports tagging the registration.
    /// </summary>
    /// <param name="push">The push manager.</param>
    public static bool IsTagsSupport(this IPushManager push)
        => push.Tags != null;


    /// <summary>
    /// Sets the supplied tags on the registration if tag support is available.
    /// </summary>
    /// <param name="pushManager">The push manager.</param>
    /// <param name="tags">The tag set to apply.</param>
    /// <returns>True when tags were set; false if the provider does not support tagging.</returns>
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


    /// <summary>
    /// Returns the currently registered tags if the provider supports tagging, otherwise null.
    /// </summary>
    /// <param name="pushManager">The push manager.</param>
    public static string[]? TryGetTags(this IPushManager pushManager)
        => pushManager.Tags?.RegisteredTags;


    /// <summary>
    /// Requests push access and, when the provider supports tagging, applies the supplied tags to the registration.
    /// </summary>
    /// <param name="pushManager">The push manager.</param>
    /// <param name="tags">Tags to set after a successful registration.</param>
    /// <returns>The resulting push access state.</returns>
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
