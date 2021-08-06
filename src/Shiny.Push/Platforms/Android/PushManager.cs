using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Shiny.Notifications;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager,
                                      IPushTagSupport,
                                      IShinyStartupTask
    {
        readonly Subject<PushNotification> receiveSubj;
        readonly IAndroidContext context;
        readonly INotificationManager notificationManager;
        readonly AndroidPushProcessor processor;
        readonly PushContainer container;
        readonly FirebaseManager firebase;
        readonly ILogger logger;


        public PushManager(IAndroidContext context,
                           INotificationManager notificationManager,
                           FirebaseManager firebase,
                           AndroidPushProcessor processor,
                           PushContainer container,
                           ILogger<IPushManager> logger)
        {
            this.context = context;
            this.notificationManager = notificationManager;
            this.firebase = firebase;
            this.processor = processor;
            this.container = container;
            this.logger = logger;
            this.receiveSubj = new Subject<PushNotification>();
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
        public string[]? RegisteredTags => this.container.RegisteredTags;


        public void Start()
        {
            this.context
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


        public async Task UnRegister()
        {
            this.container.ClearRegistration();
            await this.firebase.UnRegister();
        }


        public IObservable<PushNotification> WhenReceived()
            => this.receiveSubj;


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
