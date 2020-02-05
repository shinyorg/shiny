#if !NETSTANDARD20
using System;
using System.Threading.Tasks;
using Shiny.Push;
using Shiny.Push.AzureNotifications;
using Shiny.Settings;
using Microsoft.Azure.NotificationHubs;


namespace Shiny.Integrations.AzureNotifications
{
    public class PushManager : Shiny.Push.PushManager
    {
        readonly NotificationHubClient hub;


        public PushManager(AzureNotificationConfig config, ISettings settings) : base(settings)
        {
            this.hub = new NotificationHubClient(
                config.HubName,
                config.ListenerConnectionString,
                new NotificationHubClientSettings()
            );
        }

        public override async Task<PushAccessState> RequestAccess()
        {
            var access = await base.RequestAccess();
            if (access.Status == AccessState.Available)
            {
                try
                {
                    var regId = await this.CreateRegistration(access.RegistrationToken);
                    access = new PushAccessState(AccessState.Available, regId);
                    //this.CurrentRegistrationToken = regId;
                }
                catch
                {
                    // wipe out vars
                    //access = new PushAccessState(AccessState.Unknown, null);
                    throw;
                }
            }
            return access;
        }


#if XAMARIN_IOS
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateAppleNativeRegistrationAsync(accessToken);
            //this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#elif WINDOWS_UWP
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateWindowsNativeRegistrationAsync(accessToken);
            //this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#else
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateFcmNativeRegistrationAsync(accessToken);
            //this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#endif


    }
}
#endif