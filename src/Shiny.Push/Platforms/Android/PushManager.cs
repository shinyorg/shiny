using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
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
        readonly INotificationManager notificationManager;
        public PushManager(ShinyCoreServices services, INotificationManager notificationManager) : base(services)
            => this.notificationManager = notificationManager;


        public virtual void Start()
        {
            // wireup firebase if it was active
            FirebaseMessaging.Instance.AutoInitEnabled = this.CurrentRegistrationToken != null;

            ShinyFirebaseService
                .WhenTokenChanged()
                .Subscribe(token =>
                {
                    if (this.CurrentRegistrationToken != null)
                    {
                        this.CurrentRegistrationToken = token;
                        this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                    }
                });
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
            await Task.Run(() => FirebaseInstanceId.Instance.DeleteInstanceId());
        }


        public override IObservable<IDictionary<string, string>> WhenReceived()
            => ShinyFirebaseService.WhenDataReceived();


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
    }
}
