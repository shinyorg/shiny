using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushTagSupport : IPushManager
    {
        Task AddTag(string tag);
        Task RemoveTag(string tag);
        Task ClearTags();

        string[]? RegisteredTags { get; }
    }
}
