using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Optional push provider capability for managing tags associated with the current registration (e.g. Azure Notification Hubs tags).
/// </summary>
public interface IPushTagSupport : IPushProvider
{
    /// <summary>
    /// Replaces all currently registered tags with the supplied set.
    /// </summary>
    /// <param name="tags">The new tag set, or null/empty to clear.</param>
    Task SetTags(params string[]? tags);

    /// <summary>
    /// Adds a single tag to the currently registered set.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    Task AddTag(string tag);

    /// <summary>
    /// Removes a single tag from the currently registered set.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    Task RemoveTag(string tag);


    /// <summary>
    /// Clears all registered tags for the current registration.
    /// </summary>
    Task ClearTags();


    /// <summary>
    /// The set of tags currently registered with the provider, or null if none.
    /// </summary>
    string[]? RegisteredTags { get; }
}
