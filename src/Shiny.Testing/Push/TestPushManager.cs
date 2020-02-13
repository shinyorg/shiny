using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushManager : IPushManager
    {
        public Subject<TestPushNotification> NotificationSubject { get; } = new Subject<TestPushNotification>();
        public IObservable<IPushNotification> WhenReceived() => this.NotificationSubject;
        public AccessState ResultStatus { get; set; } = AccessState.Available;
        public DateTime? CurrentRegistrationTokenDate { get; set; }
        public string? CurrentRegistrationToken { get; set; }

        public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(new PushAccessState(this.ResultStatus, this.CurrentRegistrationToken));
        public Task UnRegister() => Task.CompletedTask;
    }
}
