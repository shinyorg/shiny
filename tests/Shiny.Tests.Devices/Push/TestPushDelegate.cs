using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Tests.Devices.Push
{
    public class TestPushDelegate : IPushDelegate
    {
        public Task OnEntry(PushEntryArgs args) => throw new NotImplementedException();
        public Task OnReceived(IDictionary<string, string> data) => throw new NotImplementedException();
        public Task OnTokenChanged(string token) => throw new NotImplementedException();
    }
}
