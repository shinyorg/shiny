using System;
using System.Threading.Tasks;
using Shiny.Push.AzureNotificationHubs.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public class NativeAdapter : INativeAdapter, IShinyStartupTask
    {
        readonly FirebaseManager firebase;
        public NativeAdapter(FirebaseManager firebase) => this.firebase = firebase;


        public Task<PushAccessState> RequestAccess() => throw new NotImplementedException();
        public void Start() => throw new NotImplementedException();
        public Task UnRegister() => throw new NotImplementedException();
    }
}
