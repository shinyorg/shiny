using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushManager : IPushManager, IPushTagSupport
    {
        public Subject<IDictionary<string, string>> NotificationSubject { get; } = new Subject<IDictionary<string, string>>();
        public IObservable<IDictionary<string, string>> WhenReceived() => this.NotificationSubject;
        public AccessState ResultStatus { get; set; } = AccessState.Available;
        public DateTime? CurrentRegistrationTokenDate { get; set; }
        public string? CurrentRegistrationToken { get; set; }
        public DateTime? CurrentRegistrationExpiryDate { get; set; }
        public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(new PushAccessState(this.ResultStatus, this.CurrentRegistrationToken));
        public Task UnRegister() => Task.CompletedTask;

        public string[]? RegisteredTags { get; private set; }
        public Task SetTags(params string[] tags)
        {
            this.RegisteredTags = tags;
            return Task.CompletedTask;
        }
    }
}
