using System;
using System.Threading.Tasks;
using Shiny.Push.AzureNotificationHubs.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public class NativeAdapter : INativeAdapter, IShinyStartupTask
    {
        public NativeAdapter() { }


        public Task<PushAccessState> RequestAccess() => throw new NotImplementedException();
        public void Start() => throw new NotImplementedException();
        public Task UnRegister() => throw new NotImplementedException();
    }
}
