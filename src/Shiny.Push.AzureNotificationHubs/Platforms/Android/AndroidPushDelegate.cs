using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Push.AzureNotificationHubs
{
    public class AndroidPushDelegate : IPushDelegate
    {
        readonly IPushManager pushManager;
        public AndroidPushDelegate(IPushManager pushManager) => this.pushManager = pushManager;
        public Task OnEntry(PushEntryArgs args) => Task.CompletedTask;
        public Task OnReceived(IDictionary<string, string> data) => Task.CompletedTask;
        public Task OnTokenChanged(string token)
            => ((IAzurePushManager)this.pushManager).UpdateRegistrationToken(token);
    }
}
