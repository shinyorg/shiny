using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    /// <summary>
    /// If you only care about foreground push notifications or you use an appservice library like
    /// Shiny.GeoDispatch, you don't need your own delegate, but push requires one to register in startup, so use this instead
    /// </summary>
    public class PushDelegate : IPushDelegate
    {
        public virtual Task OnEntry(PushNotificationResponse response) => Task.CompletedTask;
        public virtual Task OnReceived(PushNotification notification) => Task.CompletedTask;
        public virtual Task OnTokenRefreshed(string token) => Task.CompletedTask;
    }
}
