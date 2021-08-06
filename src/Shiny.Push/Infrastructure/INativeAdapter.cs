using System;
using System.Threading.Tasks;


namespace Shiny.Push.Infrastructure
{
    public interface INativeAdapter
    {
        Task<PushAccessState> RequestAccess();
        Task UnRegister();

        Action<PushNotification>? OnReceived { get; set; }
        Action<PushNotificationResponse>? OnResponse { get; set; }
        Action<string>? OnTokenRefreshed { get; set; }
        // TODO: register tags/properties
    }
}
