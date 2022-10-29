using System;
using System.Threading.Tasks;

namespace Shiny.Push.Infrastructure;


public interface INativeAdapter
{
    Task<PushAccessState> RequestAccess();
    Task UnRegister();

    Func<PushNotification, Task>? OnReceived { get; set; }
    Func<PushNotification, Task>? OnEntry { get; set; }
}
