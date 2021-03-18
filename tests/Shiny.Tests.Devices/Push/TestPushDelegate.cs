using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Tests.Devices.Push
{
    public class TestPushDelegate : IPushDelegate
    {
        public Action<PushEntryArgs>? Entry { get; set; }
        public Task OnEntry(PushEntryArgs args)
        {
            this.Entry?.Invoke(args);
            return Task.CompletedTask;
        }


        public Action<IDictionary<string, string>>? Received { get; set; }
        public Task OnReceived(IDictionary<string, string> data)
        {
            this.Received?.Invoke(data);
            return Task.CompletedTask;
        }


        public Action<string>? TokenChanged { get; set; }
        public Task OnTokenChanged(string token)
        {
            this.TokenChanged?.Invoke(token);
            return Task.CompletedTask;
        }
}
}
