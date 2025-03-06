using System.Threading.Tasks;

namespace Shiny.Push;


public interface IPushTagSupport : IPushProvider
{
    /// <summary>
    /// This will clear all current tags and set the new array
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    Task SetTags(params string[]? tags);

    /// <summary>
    /// Adds a tag to the current registered set of tags
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    Task AddTag(string tag);

    /// <summary>
    /// Remove a tag to the current registered set of tags
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    Task RemoveTag(string tag);


    /// <summary>
    /// Clears all registered tags
    /// </summary>
    /// <returns></returns>
    Task ClearTags();


    /// <summary>
    /// Current set of registered tagsw
    /// </summary>
    string[]? RegisteredTags { get; }
}
