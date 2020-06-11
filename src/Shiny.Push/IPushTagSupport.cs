using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushTagSupport
    {
        Task SetTags(params string[] tags);
        string[]? RegisteredTags { get; }
    }
}
