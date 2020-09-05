using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;


public class PushDelegate : IPushDelegate
{
    public async Task OnEntry(PushEntryArgs args)
    {
        // fires when the user taps on a push notification
    }

    public async Task OnReceived(IDictionary<string, string> data)
    {
        // fires when a push notification is received (silient or notification)
    }

    public async Task OnTokenChanged(string token)
    {
        // fires when a push notification change is set by the operating system or provider
    }
}