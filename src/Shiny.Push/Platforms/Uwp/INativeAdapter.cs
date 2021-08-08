using System;
using System.Threading.Tasks;

using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        public Func<PushNotification, Task>? OnReceived { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Func<PushNotificationResponse, Task>? OnResponse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Func<string, Task>? OnTokenRefreshed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<PushAccessState> RequestAccess() => throw new NotImplementedException();
        public Task UnRegister() => throw new NotImplementedException();
    }
}
