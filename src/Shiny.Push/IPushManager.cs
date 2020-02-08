using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushManager
    {
        DateTime? CurrentRegistrationTokenDate { get; }
        string? CurrentRegistrationToken { get; }
        Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);
        Task UnRegister();
    }
}
