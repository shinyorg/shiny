using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Notifications;
using Shiny.Settings;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : Shiny.Push.PushManager, 
                               IPushTagSupport
    {
        public PushManager(ISettings settings, IServiceProvider services, iOSNotificationDelegate ndelegate)
            : base(settings, services, ndelegate)
        {
            Messaging.SharedInstance.AutoInitEnabled = true;
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


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);
            if (access.Status == AccessState.Available)
            {
                Messaging.SharedInstance.ApnsToken = access.RegistrationToken;

                var result = await InstanceId.SharedInstance.GetInstanceIdAsync();
                this.CurrentRegistrationToken = result.Token;
                this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            }
            return access;
        }
        

        public override async Task UnRegister()
        {
            await InstanceId.SharedInstance.DeleteIdAsync();
            await base.UnRegister();
        }


        public async Task SetTags(params string[] tags)
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
