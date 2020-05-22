#if !NETSTANDARD2_0
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.NotificationHubs;
using Shiny.Push;
using Shiny.Push.AzureNotifications;
using Shiny.Settings;


namespace Shiny.Integrations.AzureNotifications
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
                        var reg = await this.CreateAnhToken(access, tags, cancelToken);
                        this.NativeRegistrationToken = access.RegistrationToken;
                        this.CurrentRegistrationExpiryDate = reg.ExpirationTime;
                        this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                        this.CurrentRegistrationToken = reg.RegistrationId;
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
            await this.hub.DeleteRegistrationAsync(this.CurrentRegistrationToken);
            this.RegisteredTags = null;
            await base.UnRegister();
        }


        public async Task UpdateTags(params string[] tags)
        {
            var reg = await this.GetCurrentAnhToken(CancellationToken.None);
            reg.Tags?.Clear();

            foreach (var tag in tags)
            {
                if (!tag.IsEmpty())
                {
                    reg.Tags ??= new HashSet<string>();
                    reg.Tags.Add(tag);
                }
            }

            await this.hub.UpdateRegistrationAsync(reg);
            this.RegisteredTags = tags;
        }


        protected virtual Task<RegistrationDescription> GetCurrentAnhToken(CancellationToken cancelToken)
        {
            return this.hub.GetRegistrationAsync<RegistrationDescription>(this.CurrentRegistrationToken, cancelToken);
//#if WINDOWS_UWP
//            return await this.hub.GetRegistrationAsync<WindowsRegistrationDescription>(this.CurrentRegistrationToken, cancelToken);
//#elif __IOS__
//            return await this.hub.GetRegistrationAsync<AppleRegistrationDescription>(this.CurrentRegistrationToken, cancelToken);
//#elif __ANDROID__
//            return await this.hub.GetRegistrationAsync<FcmRegistrationDescription>(this.CurrentRegistrationToken, cancelToken);
//#endif
        }


        protected virtual async Task<RegistrationDescription> CreateAnhToken(PushAccessState access, string[] tags, CancellationToken cancelToken)
        {
#if WINDOWS_UWP
            return await this.hub.CreateWindowsNativeRegistrationAsync(access.RegistrationToken, tags, cancelToken);
#elif __IOS__
            return await this.hub.CreateAppleNativeRegistrationAsync(access.RegistrationToken, tags, cancelToken);
#elif __ANDROID__
            return await this.hub.CreateFcmNativeRegistrationAsync(access.RegistrationToken, tags, cancelToken);
#endif
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