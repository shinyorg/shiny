using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Shiny.Notifications;
using Shiny.Push.Infrastructure;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager,
                                      IPushTagSupport,
                                      IShinyStartupTask
    {
        readonly INotificationManager notificationManager;
        readonly PushContainer container;
        readonly INativeAdapter adapter;


        public PushManager(INotificationManager notificationManager,
                           INativeAdapter adapter,
                           PushContainer container)
        {
            this.notificationManager = notificationManager;
            this.adapter = adapter;
            this.container = container;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
        public string[]? RegisteredTags => this.container.RegisteredTags;


        public void Start()
        {
            this.adapter.OnTokenRefreshed = async token =>
            {
                this.container.SetCurrentToken(token, false);
                await this.container.OnTokenRefreshed(token).ConfigureAwait(false);
            };

            this.adapter.OnReceived = async push =>
            {
                await this.container.OnReceived(push);
                if (push.Notification != null)
                    await this.notificationManager.Send(push.Notification);
            };

            this.adapter.OnResponse = push => this.container.OnEntry(push);

            // wireup firebase if it was active
            if (this.CurrentRegistrationToken != null)
                FirebaseMessaging.Instance.AutoInitEnabled = true;
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var nresult = await this.notificationManager.RequestAccess();
            if (nresult != AccessState.Available)
                return new PushAccessState(nresult, null);

            var result = await this.adapter.RequestAccess().ConfigureAwait(false);
            this.container.SetCurrentToken(result.RegistrationToken!, false);

            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public async Task UnRegister()
        {
            this.container.ClearRegistration();
            await this.adapter.UnRegister();
        }


        public IObservable<PushNotification> WhenReceived()
            => this.container.WhenReceived();


        public async Task AddTag(string tag)
        {
            var tags = this.container.RegisteredTags?.ToList() ?? new List<string>(1);
            tags.Add(tag);

            await FirebaseMessaging.Instance.SubscribeToTopic(tag);
            this.container.RegisteredTags = tags.ToArray();
        }


        public async Task RemoveTag(string tag)
        {
            var list = this.container.RegisteredTags?.ToList() ?? new List<string>(0);
            if (list.Remove(tag))
                this.container.RegisteredTags = list.ToArray();

            await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);
        }


        public async Task ClearTags()
        {
            if (this.container.RegisteredTags != null)
                foreach (var tag in this.container.RegisteredTags)
                    await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);

            this.container.RegisteredTags = null;
        }


        public async Task SetTags(params string[]? tags)
        {
            await this.ClearTags();
            if (tags != null)
                foreach (var tag in tags)
                    await this.AddTag(tag);
        }
    }
}
