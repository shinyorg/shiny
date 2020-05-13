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
    public class PushManager : Shiny.Push.PushManager, IPushTagEnabled
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
                        await this.CreateRegistration(access.RegistrationToken, tags, cancelToken);
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
            this.RegisteredTags = null;
            await this.hub.DeleteRegistrationAsync(this.CurrentRegistrationToken);
            await base.UnRegister();
        }


        public async Task UpdateTags(params string[] tags)
        {
            var registrations = await this.hub.GetAllRegistrationsAsync(10);
            foreach (var reg in registrations)
            {
                reg.Tags.Clear();
                foreach (var tag in tags)
                    reg.Tags.Add(tag);

                await hub.UpdateRegistrationAsync(reg);
            }
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

#if XAMARIN_IOS
        protected virtual async Task CreateRegistration(string accessToken, string[] tags, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateAppleNativeRegistrationAsync(accessToken, tags, cancelToken);
            this.CurrentRegistrationExpiryDate = reg.ExpirationTime;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.CurrentRegistrationToken = reg.RegistrationId;
            this.RegisteredTags = tags;
        }
#elif WINDOWS_UWP
        protected virtual async Task CreateRegistration(string accessToken, string[] tags, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateWindowsNativeRegistrationAsync(accessToken, tags, cancelToken);
            this.CurrentRegistrationExpiryDate = reg.ExpirationTime;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.CurrentRegistrationToken = reg.RegistrationId;
            this.RegisteredTags = tags;
        }

#else
        protected virtual async Task CreateRegistration(string accessToken, string[] tags, CancellationToken cancelToken)
        {
            var reg = await this.hub.CreateFcmNativeRegistrationAsync(accessToken, tags, cancelToken);
            this.CurrentRegistrationExpiryDate = reg.ExpirationTime;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.CurrentRegistrationToken = reg.RegistrationId;
            this.RegisteredTags = tags;
        }
#endif
    }
}
#endif