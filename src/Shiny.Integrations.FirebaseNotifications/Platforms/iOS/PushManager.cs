using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Logging;
using Shiny.Push;
using Shiny.Settings;


namespace Shiny.Integrations.FirebaseNotifications
{
    public class PushManager : Shiny.Push.PushManager
    {
        readonly Lazy<IPushDelegate> pushDelegate = ShinyHost.LazyResolve<IPushDelegate>();


        public PushManager(ISettings settings) : base(settings)
        {
            Messaging.SharedInstance.Delegate = new FbMessagingDelegate
            (
                async msg =>
                {
                    var dict = msg.AppData.FromNsDictionary();
                    await Log.SafeExecute(async () => await this.pushDelegate.Value.OnReceived(dict));
                },
                async token =>
                {
                    this.CurrentRegistrationToken = token;
                    this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                    await Log.SafeExecute(async () => await this.pushDelegate.Value.OnTokenChanged(token));
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
