using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Infrastructure;
using Shiny.Notifications;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        public PushManager(ShinyCoreServices services, iOSNotificationDelegate ndelegate) : base(services, ndelegate)
        {
        }


        public override void Start()
        {
            base.Start();

            Firebase.Core.App.Configure();
            //Messaging.Notifications.ObserveMessagesDeleted
            Messaging.SharedInstance.AutoInitEnabled = true;
            Messaging.SharedInstance.Delegate = new FbMessagingDelegate
            (
                async msg =>
                {
                    var dict = msg.AppData.FromNsDictionary();
                    await this.Services.Services.RunDelegates<IPushDelegate>(x => x.OnReceived(dict));
                },
                async token =>
                {
                    this.CurrentRegistrationToken = token;
                    this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                    await this.Services.Services.RunDelegates<IPushDelegate>(x => x.OnTokenChanged(token));
                }
            );
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);
            if (access.Status != AccessState.Available)
                return access;

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
            var tags = this.RegisteredTags.ToList();
            tags.Add(tag);

            await Messaging.SharedInstance.SubscribeAsync(tag);
            this.RegisteredTags = tags.ToArray();
        }


        public async Task RemoveTag(string tag)
        {
            await Messaging.SharedInstance.UnsubscribeAsync(tag);
            var tags = this.RegisteredTags.ToList();
            if (tags.Remove(tag))
                this.RegisteredTags = tags.ToArray();
        }


        public async Task ClearTags()
        {
            foreach (var tag in this.RegisteredTags)
                await Messaging.SharedInstance.UnsubscribeAsync(tag);

            this.RegisteredTags = null;
        }
    }
}
