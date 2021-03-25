using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;
using Shiny.Notifications;


namespace Shiny.Testing.Push
{
    public class TestPushDelegate : IPushDelegate
    {
        public static Action<IDictionary<string, string>, Notification?, bool>? Action { get; set; }
        public Task OnAction(IDictionary<string, string> data, Notification? notification, bool entry)
        {
            Action?.Invoke(data, notification, entry);
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
