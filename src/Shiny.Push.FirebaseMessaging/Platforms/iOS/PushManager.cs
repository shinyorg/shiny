using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : IPushManager, IPushTagSupport
    {
        readonly ApnManager apnManager;
        readonly PushContainer container;


        public PushManager(ApnManager apnManager, PushContainer container)
        {
            this.apnManager = apnManager;
            this.container = container;
        }


        public void Start()
        {
            //if (this.CurrentRegistrationToken != null)
            //    this.TryStartFirebase();
        }


        protected virtual void TryStartFirebase()
        {
            if (Messaging.SharedInstance == null)
                Firebase.Core.App.Configure();

            Messaging.SharedInstance!.AutoInitEnabled = true;
            Messaging.SharedInstance.Delegate = new FbMessagingDelegate
            (
                async msg =>
                {
                    // I can't get access to the notification here
                    var dict = msg.AppData.FromNsDictionary();
                    var pr = new PushNotification(dict, null);
                    await this.container.OnReceived(pr).ConfigureAwait(false);
                },
                async token =>
                {
                    this.container.SetCurrentToken(token);
                    await this.container.OnTokenRefreshed(token).ConfigureAwait(false);
                }
            );
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            //var access = await base.RequestAccess(cancelToken);
            //if (access.Status != AccessState.Available)
            //    return access;

            //this.TryStartFirebase();
            //Messaging.SharedInstance.ApnsToken = access.RegistrationToken;
            ////Messaging.SharedInstance.RetrieveFcmTokenAsync()
            //var result = await InstanceId.SharedInstance.GetInstanceIdAsync();
            //this.CurrentRegistrationToken = result.Token;
            //this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, result.Token);
        }


        public async Task UnRegister()
        {
            await InstanceId.SharedInstance.DeleteIdAsync().ConfigureAwait(false);
            await this.apnManager.UnRegister().ConfigureAwait(false);
        }


        public async Task AddTag(string tag)
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
            tags.Add(tag);

            await Messaging.SharedInstance.SubscribeAsync(tag);
            this.RegisteredTags = tags.ToArray();
        }


        public async Task RemoveTag(string tag)
        {
            await Messaging.SharedInstance.UnsubscribeAsync(tag);
            if (this.RegisteredTags != null)
            {
                var tags = this.RegisteredTags.ToList();
                if (tags.Remove(tag))
                    this.RegisteredTags = tags.ToArray();
            }
        }


        public async Task ClearTags()
        {
            if (this.RegisteredTags != null)
                foreach (var tag in this.RegisteredTags)
                    await Messaging.SharedInstance.UnsubscribeAsync(tag);

            this.RegisteredTags = null;
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
