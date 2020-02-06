#if !NETSTANDARD2_0
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

#if WINDOWS_UWP
        public PushManager(AzureNotificationConfig config, IServiceProvider serviceProvider, ISettings settings) : base(serviceProvider, settings)
#elif __ANDROID__
        public PushManager(AzureNotificationConfig config, ISettings settings, AndroidContext context) : base(settings, context)
#else
        public PushManager(AzureNotificationConfig config, ISettings settings) : base(settings)
#endif
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
                    this.CurrentRegistrationToken = await this.CreateRegistration(access.RegistrationToken);
                    access = new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
                }
                catch
                {
                    this.CurrentRegistrationToken = null;
                    this.CurrentRegistrationTokenDate = null;
                    throw;
                }
            }
            return access;
        }


        //public override async Task UnRegister()
        //{
        //    await base.UnRegister();
        //}

#if XAMARIN_IOS
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateAppleNativeRegistrationAsync(accessToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#elif WINDOWS_UWP
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateWindowsNativeRegistrationAsync(accessToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#else
        protected virtual async Task<string> CreateRegistration(string accessToken)
        {
            var reg = await this.hub.CreateFcmNativeRegistrationAsync(accessToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#endif
    }
}
#endif