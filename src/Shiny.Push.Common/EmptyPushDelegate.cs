using System;
using System.Threading.Tasks;


namespace Shiny.Push.Common
{
    /// <summary>
    /// If you only care about foreground push notifications or you use an appservice library like
    /// Shiny.GeoDispatch, you don't need your own delegate, but push requires one to register in startup, so use this instead
    /// </summary>
    public class EmptyPushDelegate : IPushDelegate
    {
        public Task OnEntry(PushNotificationResponse response) => Task.CompletedTask;
        public Task OnReceived(PushNotification notification) => Task.CompletedTask;
        public Task OnTokenRefreshed(string token) => Task.CompletedTask;
    }
}
