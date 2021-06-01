using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Infrastructure;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        public PushManager(ShinyCoreServices services) : base(services) { }


        public override void Start()
        {
            base.Start();

            if (this.CurrentRegistrationToken != null)
                this.TryStartFirebase();
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
                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(
                            x => x.OnReceived(new PushNotification(dict, null))
                        )
                        .ConfigureAwait(false);
                },
                async token =>
                {
                    this.CurrentRegistrationToken = token;
                    this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnTokenChanged(token))
                        .ConfigureAwait(false);
                }
            );
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);
            if (access.Status != AccessState.Available)
                return access;

            this.TryStartFirebase();
            Messaging.SharedInstance.ApnsToken = access.RegistrationToken;
            //Messaging.SharedInstance.RetrieveFcmTokenAsync()
            var result = await InstanceId.SharedInstance.GetInstanceIdAsync();
            this.CurrentRegistrationToken = result.Token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, result.Token);
        }


        public override async Task UnRegister()
        {
            await InstanceId.SharedInstance.DeleteIdAsync();
            await base.UnRegister();
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
