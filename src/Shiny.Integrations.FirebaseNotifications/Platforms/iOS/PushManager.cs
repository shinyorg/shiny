using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Shiny.Push;
using Shiny.Settings;
using Shiny.Notifications;


namespace Shiny.Integrations.FirebaseNotifications
{
    public class PushManager : Shiny.Push.PushManager
    {
        public PushManager(ShinyNotificationContext context, ISettings settings) : base(context, settings)
        {
            Messaging.SharedInstance.Delegate = new FbMessagingDelegate();
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
