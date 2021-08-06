using System;
using System.Threading.Tasks;


namespace Shiny.Push.AzureNotificationHubs.Infrastructure
{
    public interface INativeAdapter
    {
        Task<PushAccessState> RequestAccess();
        Task UnRegister();
    }
}
