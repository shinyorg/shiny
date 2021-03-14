using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Android.Gms.Extensions;
using Firebase.Iid;
using Firebase.Messaging;
using Shiny.Notifications;
using Shiny.Infrastructure;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IPushTagSupport, IAndroidTokenUpdate
    {
        readonly INotificationManager notificationManager;


        public PushManager(ShinyCoreServices services, INotificationManager notificationManager) : base(services)
        {
            this.notificationManager = notificationManager;
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            //var resultCode = GoogleApiAvailability
            //    .Instance
            //    .IsGooglePlayServicesAvailable(this.context.AppContext);

            //if (resultCode == ConnectionResult.)
            //if (resultCode != ConnectionResult.ServiceMissing)
            //{
            ////{
            ////    if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
            ////        msgText.Text = GoogleApiAvailability.Instance.GetErrorString(resultCode);
            //}
            var nresult = await this.notificationManager.RequestAccess();
            if (nresult != AccessState.Available)
                return new PushAccessState(nresult, null);

            FirebaseMessaging.Instance.AutoInitEnabled = true;
            var result = await FirebaseInstanceId
                .Instance
                .GetInstanceId()
                .AsAsync<IInstanceIdResult>();

            this.CurrentRegistrationToken = result.Token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            this.ClearRegistration();
            FirebaseMessaging.Instance.AutoInitEnabled = false;

            // must be executed off proc
            await Task.Run(() => FirebaseInstanceId.Instance.DeleteInstanceId());
        }


        public override IObservable<IDictionary<string, string>> WhenReceived()
            => this.Services.Bus.Listener<IDictionary<string, string>>(nameof(ShinyFirebaseService));



        public virtual Task UpdateNativePushToken(string token)
        {
            this.CurrentRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return Task.CompletedTask;
        }


        public virtual async Task AddTag(string tag)
        {
            var tags = this.RegisteredTags.ToList();
            tags.Add(tag);

            await FirebaseMessaging.Instance.SubscribeToTopic(tag);
            this.RegisteredTags = tags.ToArray();
        }


        public virtual async Task RemoveTag(string tag)
        {
            var list = this.RegisteredTags.ToList();
            if (list.Remove(tag))
                this.RegisteredTags = list.ToArray();

            await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);
        }


        public virtual async Task ClearTags()
        {
            foreach (var tag in this.RegisteredTags)
                await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);

            this.RegisteredTags = null;
        }
    }
}
