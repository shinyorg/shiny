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
        public Subject<PushNotification> ReceiveSubject { get; } = new Subject<PushNotification>();
        public IObservable<PushNotification> WhenReceived() => this.ReceiveSubject;
        public AccessState ResultStatus { get; set; } = AccessState.Available;
        public DateTime? CurrentRegistrationTokenDate { get; set; }
        public string? CurrentRegistrationToken { get; set; }
        public DateTime? CurrentRegistrationExpiryDate { get; set; }
        public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(new PushAccessState(this.ResultStatus, this.CurrentRegistrationToken));
        public Task UnRegister() => Task.CompletedTask;


        readonly List<string> tags = new List<string>();
        public string[]? RegisteredTags => this.tags.ToArray();


        public Task AddTag(string tag)
        {
            this.tags.Add(tag);
            return Task.CompletedTask;
        }


        public Task RemoveTag(string tag)
        {
            this.tags.Remove(tag);
            return Task.CompletedTask;
        }


        public Task ClearTags()
        {
            this.tags.Clear();
            return Task.CompletedTask;
        }

        public Task SetTags(params string[]? tags)
        {
            this.tags.Clear();
            if (tags != null)
                this.tags.AddRange(tags);

            return Task.CompletedTask;
        }
    }
}
