#if !NETSTANDARD2_0
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Shiny.Settings;


namespace Shiny.Push.AzureNotificationHubs
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
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
                           IAndroidContext context,
                           Shiny.Notifications.INotificationManager notifications,
                           ISettings settings,
                           IMessageBus bus) : base(context, notifications, settings, bus)
#else
        public PushManager(AzureNotificationConfig config,
                           ISettings settings,
                           IMessageBus bus) : base(settings, bus)
#endif
        {
            this.hub = new NotificationHubClient(
                config.ListenerConnectionString,
                config.HubName
            );
        }


        public string? InstallationId
        {
            get => this.Settings.Get<string>(nameof(this.InstallationId));
            private set => this.Settings.Set(nameof(this.InstallationId), value);
        }


        public string? NativeRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(NativeRegistrationToken));
            protected set => this.Settings.Set(nameof(NativeRegistrationToken), value);
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);

            if (access.Status == AccessState.Available)
            {
                try
                {
                    if (this.InstallationId == null)
                        this.InstallationId = Guid.NewGuid().ToString().Replace("-", "");

                    var install = new Installation
                    {
                        InstallationId = this.InstallationId,
                        PushChannel = access.RegistrationToken,
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
            if (this.InstallationId == null)
                return;

            await this.hub.DeleteInstallationAsync(this.InstallationId);
            await base.UnRegister();
        }


        protected override void ClearRegistration()
        {
            base.ClearRegistration();
            this.InstallationId = null;
        }


        protected virtual bool IsRefreshNeeded(PushAccessState nativeToken)
        {
            if (this.CurrentRegistrationToken == null)
                return true;

            if (this.NativeRegistrationToken != nativeToken.RegistrationToken)
                return true;

            if (this.CurrentRegistrationExpiryDate < DateTime.Now)
                return true;

            return false;
        }


#if __ANDROID__
        public override async Task SetTags(params string[] tags)
#else
        public async Task SetTags(params string[] tags)
#endif
        {
            if (this.InstallationId == null)
                return;

            var install = await this.hub.GetInstallationAsync(this.InstallationId);
            install.Tags = tags.ToList();
            await this.hub.CreateOrUpdateInstallationAsync(install);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            this.RegisteredTags = tags;
        }



#if __ANDROID__
        public override async Task UpdateNativePushToken(string token)
        {
            if (this.InstallationId.IsEmpty())
                return;

            this.NativeRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            var install = await this.hub.GetInstallationAsync(this.InstallationId);
            install.PushChannel = token;
            await this.hub.CreateOrUpdateInstallationAsync(install);
        }
#endif
    }
}
#endif