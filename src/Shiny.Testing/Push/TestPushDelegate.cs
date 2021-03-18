using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushDelegate : IPushDelegate
    {
        public static Action<PushEntryArgs>? Entry { get; set; }
        public Task OnEntry(PushEntryArgs args)
        {
            Entry?.Invoke(args);
            return Task.CompletedTask;
        }


        public static Action<IDictionary<string, string>>? Received { get; set; }
        public Task OnReceived(IDictionary<string, string> data)
        {
            Received?.Invoke(data);
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
