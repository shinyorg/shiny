#if !NETSTANDARD2_0
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Shiny.Push;
using Shiny.Push.AzureNotificationHubs;
using Shiny.Push.AzureNotifications;
using Shiny.Settings;



namespace Shiny.Integrations.AzureNotifications
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport, IAzurePushManager
    {
        readonly NotificationHubClient hub;


#if WINDOWS_UWP
        public PushManager(AzureNotificationConfig config, ISettings settings) : base(settings)
#elif __IOS__
        public PushManager(AzureNotificationConfig config,
                           ISettings settings,
                           IServiceProvider services,
                           Shiny.Notifications.iOSNotificationDelegate ndelegate) : base(settings, services, ndelegate)
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


        public string[]? RegisteredTags
        {
            get => this.Settings.Get<string[]>(nameof(this.RegisteredTags));
            private set => this.Settings.Set(nameof(this.RegisteredTags), value);
        }


        public string? NativeRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(NativeRegistrationToken));
            protected set => this.Settings.Set(nameof(this.NativeRegistrationToken), value);
        }


        public override Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => this.RequestAccess(null, cancelToken);
        public async Task<PushAccessState> RequestAccess(string[] tags, CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess();

            if (access.Status == AccessState.Available)
            {
                if (!this.IsRefreshNeeded(access))
                {
                    access = new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
                }
                else
                {
                    try
                    {
                        var install = new Installation
                        {
                            InstallationId = access.RegistrationToken,
                            PushChannel = access.RegistrationToken,
                            Tags = tags?.ToList(),
#if WINDOWS_UWP
                            Platform = NotificationPlatform.Wns
#elif __IOS__
                            Platform = NotificationPlatform.Apns
#elif __ANDROID__
                            Platform = NotificationPlatform.Fcm
#endif
                        };
                        await this.hub.CreateOrUpdateInstallationAsync(install, cancelToken);

                        this.NativeRegistrationToken = access.RegistrationToken;
                        //this.CurrentRegistrationExpiryDate = reg.ExpirationTime;
                        this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                        this.CurrentRegistrationToken = access.RegistrationToken;
                        this.RegisteredTags = tags;

                        access = new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
                    }
                    catch
                    {
                        this.ClearRegistration();
                        throw;
                    }
                }
            }
            return access;
        }


        public override async Task UnRegister()
        {
            await this.hub.DeleteInstallationAsync(this.CurrentRegistrationToken);
            await base.UnRegister();
        }


        public async Task UpdateRegistrationToken(string newToken)
        {
            // the old token on CurrentRegistrationToken hasn't been updated yet and is updated AFTER the update below
            var install = await this.hub.GetInstallationAsync(this.CurrentRegistrationToken);
            install.PushChannel = newToken;
            await this.hub.CreateOrUpdateInstallationAsync(install);
        }


        public async Task UpdateTags(params string[] tags)
        {
            var install = await this.hub.GetInstallationAsync(this.CurrentRegistrationToken);
            install.Tags = tags.ToList();
            await this.hub.CreateOrUpdateInstallationAsync(install);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            this.RegisteredTags = tags;
        }


        protected override void ClearRegistration()
        {
            base.ClearRegistration();
            this.RegisteredTags = null;
        }


        protected virtual bool IsRefreshNeeded(PushAccessState nativeToken)
        {
            if (this.CurrentRegistrationToken.IsEmpty())
                return true;

            if (this.NativeRegistrationToken != nativeToken.RegistrationToken)
                return true;

            if (this.CurrentRegistrationExpiryDate < DateTime.Now)
                return true;

            return false;
        }
    }
}
#endif