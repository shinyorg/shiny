using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Android.Runtime;
using Android.Gms.Extensions;
using Firebase.Iid;
using Firebase.Messaging;
using Shiny.Notifications;
using Shiny.Infrastructure;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager,
                               IPushTagSupport,
                               IShinyStartupTask
    {
        readonly Subject<PushNotification> receiveSubj;
        readonly INotificationManager notificationManager;
        readonly ILogger logger;


        public PushManager(ShinyCoreServices services,
                           INotificationManager notificationManager,
                           ILogger<IPushManager> logger) : base(services)
        {
            this.notificationManager = notificationManager;
            this.logger = logger;
            this.receiveSubj = new Subject<PushNotification>();
        }


        public virtual void Start()
        {
            // wireup firebase if it was active
            if (this.CurrentRegistrationToken != null)
                FirebaseMessaging.Instance.AutoInitEnabled = true;

            ShinyFirebaseService.NewToken = async token =>
            {
                if (this.CurrentRegistrationToken != null)
                {
                    this.CurrentRegistrationToken = token;
                    this.CurrentRegistrationTokenDate = DateTime.UtcNow;

                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(
                            x => x.OnTokenChanged(token)
                        )
                        .ConfigureAwait(false);
                }
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


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var nresult = await this.notificationManager.RequestAccess();
            if (nresult != AccessState.Available)
                return new PushAccessState(nresult, null);

            FirebaseMessaging.Instance.AutoInitEnabled = true;

            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();

            this.CurrentRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            if (this.CurrentRegistrationToken == null)
                return;

            this.ClearRegistration();

            FirebaseMessaging.Instance.AutoInitEnabled = false;
            await Task.Run(() => FirebaseInstanceId.Instance.DeleteInstanceId()).ConfigureAwait(false);
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


        protected virtual async Task OnPushReceived(PushNotification push)
        {
            this.receiveSubj.OnNext(push);

            await this.Services.Services.RunDelegates<IPushDelegate>(
                x => x.OnReceived(push)
            );

            if (push.Notification != null)
                await this.notificationManager.Send(push.Notification);
        }


        protected virtual PushNotification FromNative(RemoteMessage message)
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
