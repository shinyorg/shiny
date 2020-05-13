using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushTagEnabled
    {
        Task<PushAccessState> RequestAccess(string[] tags, CancellationToken cancelToken = default);
    }
}
