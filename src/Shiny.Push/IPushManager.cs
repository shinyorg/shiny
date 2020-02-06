using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushManager
    {
        DateTime? CurrentRegistrationTokenDate { get; }
        string? CurrentRegistrationToken { get; }
        Task<PushAccessState> RequestAccess();
        Task UnRegister();
    }
}
