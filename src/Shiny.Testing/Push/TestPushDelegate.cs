using System;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushDelegate : IPushDelegate
    {
        public static Action<PushNotificationResponse>? Entry { get; set; }
        public Task OnEntry(PushNotificationResponse response)
        {
            Entry?.Invoke(response);
            return Task.CompletedTask;
        }


        public static Action<PushNotification>? Receive { get; set; }
        public Task OnReceived(PushNotification push)
        {
            Receive?.Invoke(push);
            return Task.CompletedTask;
        }


        public static Action<string>? TokenChanged { get; set; }
        public Task OnTokenChanged(string token)
        {
            TokenChanged?.Invoke(token);
            return Task.CompletedTask;
        }
    }
}
