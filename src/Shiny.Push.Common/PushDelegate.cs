using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public class PushDelegate : IPushDelegate
    {
        public virtual Task OnEntry(PushNotification notification) => Task.CompletedTask;
        public virtual Task OnReceived(PushNotification notification) => Task.CompletedTask;
        public virtual Task OnTokenRefreshed(string token) => Task.CompletedTask;
    }
}
