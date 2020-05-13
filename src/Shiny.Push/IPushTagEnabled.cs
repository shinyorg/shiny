using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushTagEnabled
    {
        Task<PushAccessState> RequestAccess(string[] tags, CancellationToken cancelToken = default);
        Task UpdateTags(params string[] tags);
        string[]? RegisteredTags { get; }
    }
}
