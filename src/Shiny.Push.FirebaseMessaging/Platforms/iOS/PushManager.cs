using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Settings;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : Shiny.Push.PushManager
    {
        public PushManager(ISettings settings, IServiceProvider services)
            : base(settings, services)
        {
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
                var s = Messaging.SharedInstance;
                s.ApnsToken = access.RegistrationToken;
                s.AutoInitEnabled = true;
                s.ShouldEstablishDirectChannel = true;

                var result = await InstanceId.SharedInstance.GetInstanceIdAsync();
                this.CurrentRegistrationToken = result.Token;
                this.CurrentRegistrationTokenDate = DateTime.UtcNow;
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
    }
}
