using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push.AzureNotificationHubs
{
    public class PushManager : IPushManager, IPushTagSupport
    {
        readonly Subject<PushNotification> recvSubj;
        readonly PushContainer container;


        public PushManager(PushContainer container)
        {
            this.recvSubj = new Subject<PushNotification>();
            this.container = container;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
        public string[]? RegisteredTags => this.container.RegisteredTags;

        public Task AddTag(string tag) => throw new NotImplementedException();
        public Task ClearTags() => throw new NotImplementedException();
        public Task RemoveTag(string tag) => throw new NotImplementedException();
        public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task SetTags(params string[]? tags) => throw new NotImplementedException();
        public Task UnRegister() => throw new NotImplementedException();
        public IObservable<PushNotification> WhenReceived() => this.recvSubj;
    }
}
