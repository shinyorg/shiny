using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Android.Runtime;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Shiny.Notifications;
using Shiny.Infrastructure;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager,
                                      IPushTagSupport,
                                      IShinyStartupTask
    {
        readonly Subject<PushNotification> receiveSubj;
        readonly INotificationManager notificationManager;
        readonly AndroidPushProcessor processor;
        readonly PushContainer container;
        readonly FirebaseManager firebase;
        readonly ILogger logger;


        public PushManager(ShinyCoreServices services,
                           INotificationManager notificationManager,
                           FirebaseManager firebase,
                           AndroidPushProcessor processor,
                           PushContainer container,
                           ILogger<IPushManager> logger)
        {
            this.notificationManager = notificationManager;
            this.firebase = firebase;
            this.processor = processor;
            this.container = container;
            this.logger = logger;
            this.receiveSubj = new Subject<PushNotification>();
        }


        public void Start()
        {
            this.Services
                .Android
                .WhenIntentReceived()
                .SubscribeAsync(x => this.processor.TryProcessIntent(x));

            // wireup firebase if it was active
            if (this.CurrentRegistrationToken != null)
                FirebaseMessaging.Instance.AutoInitEnabled = true;

            ShinyFirebaseService.NewToken = async token =>
            {
                this.container.SetCurrentToken(token);
                await this.container.OnTokenRefreshed(token);
            };

            ShinyFirebaseService.MessageReceived = async message =>
            {
                try
                {
                    var pr = this.FromNative(message);
                    await this.OnPushReceived(pr).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error processing received message");
                }
            };
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var nresult = await this.notificationManager.RequestAccess();
            if (nresult != AccessState.Available)
                return new PushAccessState(nresult, null);

            var token = await this.firebase.RequestToken().ConfigureAwait(false);
            this.container.SetCurrentToken(token);

            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            this.container.ClearRegistration();
            await this.firebase.UnRegister();
        }


        public override IObservable<PushNotification> WhenReceived()
            => this.receiveSubj;


        public virtual async Task AddTag(string tag)
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
            tags.Add(tag);

            await FirebaseMessaging.Instance.SubscribeToTopic(tag);
            this.RegisteredTags = tags.ToArray();
        }


        public virtual async Task RemoveTag(string tag)
        {
            var list = this.RegisteredTags?.ToList() ?? new List<string>(0);
            if (list.Remove(tag))
                this.RegisteredTags = list.ToArray();

            await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);
        }


        public virtual async Task ClearTags()
        {
            if (this.RegisteredTags != null)
                foreach (var tag in this.RegisteredTags)
                    await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);

            this.RegisteredTags = null;
        }


        public virtual async Task SetTags(params string[]? tags)
        {
            await this.ClearTags();
            if (tags != null)
                foreach (var tag in tags)
                    await this.AddTag(tag);
        }


        async Task OnPushReceived(PushNotification push)
        {
            await this.container.OnReceived(push);
            this.receiveSubj.OnNext(push);

            if (push.Notification != null)
                await this.notificationManager.Send(push.Notification);
        }


        PushNotification FromNative(RemoteMessage message)
        {
            Notification? notification = null;
            var native = message.GetNotification();

            if (native != null)
            {
                notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,
                    Channel = native.ChannelId
                };
                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;
            }
            return new PushNotification(message.Data, notification);
        }
    }
}
