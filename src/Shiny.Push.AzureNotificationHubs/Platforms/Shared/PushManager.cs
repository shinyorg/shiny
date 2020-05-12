#if !NETSTANDARD2_0
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Shiny.Push;
using Shiny.Push.AzureNotifications;
using Shiny.Settings;
using Shiny.Notifications;


namespace Shiny.Integrations.AzureNotifications
{
    public class PushManager : Shiny.Push.PushManager
    {
        readonly NotificationHubClient hub;

#if WINDOWS_UWP
        public PushManager(AzureNotificationConfig config, ISettings settings) : base(settings)
#elif __IOS__
        public PushManager(AzureNotificationConfig config,
                           ISettings settings,
                           IServiceProvider services,
                           iOSNotificationDelegate ndelegate) : base(settings, services, ndelegate)
#elif __ANDROID__
        public PushManager(AzureNotificationConfig config,
                           AndroidContext context,
                           ISettings settings,
                           IMessageBus bus) : base(context, settings, bus)
#else
        public PushManager(AzureNotificationConfig config,
                           ISettings settings,
                           IMessageBus bus) : base(settings, bus)
#endif
        {
            this.hub = new NotificationHubClient(
                config.ListenerConnectionString,
                config.HubName,
                new NotificationHubClientSettings()
            );
        }


        public string? NativeRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(NativeRegistrationToken));
            protected set => this.Settings.Set(nameof(this.NativeRegistrationToken), value);
        }


        // TODO: if expired or native reg token has changed, re-register with azure notification hubs
        // TODO: iOS startup should call remote notification
        // TODO: Start an in-memory timer to refresh if necessary?  Seems overkill
        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess();

            if (access.Status == AccessState.Available)
            {
                // TODO: add expiry date variable
                //if (access.RegistrationToken != this.NativeRegistrationToken)
                //    return;

                try
                {
                    this.CurrentRegistrationToken = await this.CreateRegistration(access.RegistrationToken, cancelToken);
                    access = new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
                }
                catch
                {
                    this.ClearRegistration();
                    throw;
                }
            }
            return access;
        }


        public override async Task UnRegister()
        {
            await this.hub.DeleteRegistrationAsync(this.CurrentRegistrationToken);
            await base.UnRegister();
        }

#if XAMARIN_IOS
        protected virtual async Task<string> CreateRegistration(string accessToken, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateAppleNativeRegistrationAsync(accessToken, cancelToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#elif WINDOWS_UWP
        protected virtual async Task<string> CreateRegistration(string accessToken, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateWindowsNativeRegistrationAsync(accessToken, cancelToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#else
        protected virtual async Task<string> CreateRegistration(string accessToken, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateFcmNativeRegistrationAsync(accessToken, cancelToken);
            this.CurrentRegistrationTokenDate = reg.ExpirationTime;
            return reg.RegistrationId;
        }
#endif
    }
}
#endif