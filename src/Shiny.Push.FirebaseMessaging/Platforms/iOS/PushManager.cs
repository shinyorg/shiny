using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Notifications;
using Shiny.Settings;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        public PushManager(ISettings settings, IServiceProvider services, iOSNotificationDelegate ndelegate)
            : base(settings, services, ndelegate)
        {
            // TODO: this is currently null
            Messaging.SharedInstance.Delegate = new FbMessagingDelegate
            (
                async msg =>
                {
                    var dict = msg.AppData.FromNsDictionary();
                    await services.SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(dict));
                },
                async token =>
                {
                    this.CurrentRegistrationToken = token;
                    this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                    await services.SafeResolveAndExecute<IPushDelegate>(x => x.OnTokenChanged(token));
                }
            );
        }


        public override Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => this.RequestAccess(null, cancelToken);
        public async Task<PushAccessState> RequestAccess(string[] tags, CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);
            if (access.Status == AccessState.Available)
            {
                var s = Messaging.SharedInstance;
                s.ApnsToken = access.RegistrationToken;
                s.AutoInitEnabled = true;
                s.ShouldEstablishDirectChannel = true;

                var result = await InstanceId.SharedInstance.GetInstanceIdAsync();
                this.CurrentRegistrationToken = result.Token;
                this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                await this.UpdateTags(tags);
            }
            return access;
        }
        

        public override async Task UnRegister()
        {
            var s = Messaging.SharedInstance;
            s.AutoInitEnabled = false;
            s.ShouldEstablishDirectChannel = false;

            await InstanceId.SharedInstance.DeleteIdAsync();
            await base.UnRegister();
        }


        public async Task UpdateTags(params string[] tags)
        {
            if (this.RegisteredTags != null)
            {
                foreach (var tag in this.RegisteredTags)
                {
                    await Messaging.SharedInstance.UnsubscribeAsync(tag);
                }
            }
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    await Messaging.SharedInstance.SubscribeAsync(tag);
                }
            }
        }
    }
}
