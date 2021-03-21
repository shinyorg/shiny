#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Shiny.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        readonly NotificationHubClient hub;


#if WINDOWS_UWP
        public PushManager(AzureNotificationConfig config, ShinyCoreServices services) : base(services)
#elif __IOS__
        public PushManager(AzureNotificationConfig config,
                           ShinyCoreServices services,
                           Shiny.Notifications.iOSNotificationDelegate ndelegate) : base(services, ndelegate)
#elif __ANDROID__
        public PushManager(AzureNotificationConfig config,
                           ShinyCoreServices services,
                           Shiny.Notifications.INotificationManager notifications)
                           : base(services, notifications)
#else
        public PushManager(AzureNotificationConfig config, ShinyCoreServices services) : base(services)
#endif
        {
            this.hub = new NotificationHubClient(
                config.ListenerConnectionString,
                config.HubName
            );
        }


#if __ANDROID__
        public override void Start() =>
            ShinyFirebaseService
                .WhenTokenChanged()
                .SubscribeAsync(async token =>
                {
                    try
                    {
                        this.NativeRegistrationToken = token;
                        this.CurrentRegistrationTokenDate = DateTime.UtcNow;

                        if (this.InstallationId != null)
                        {
                            var install = await this.hub.GetInstallationAsync(this.InstallationId);
                            install.PushChannel = token;
                            await this.hub.CreateOrUpdateInstallationAsync(install);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO
                    }
                });
#endif

        public string? InstallationId
        {
            get => this.Settings.Get<string>(nameof(this.InstallationId));
            private set => this.Settings.SetOrRemove(nameof(this.InstallationId), value);
        }


        public string? NativeRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(NativeRegistrationToken));
            protected set => this.Settings.SetOrRemove(nameof(NativeRegistrationToken), value);
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
            if (this.InstallationId != null)
            {
                await this.hub.DeleteInstallationAsync(this.InstallationId);
                this.InstallationId = null;
            }
            await base.UnRegister();
        }


#if __ANDROID__
        public override Task AddTag(string tag)
#else
        public Task AddTag(string tag)
#endif
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
            tags.Add(tag);
            return this.SetTags(tags.ToArray());
        }


#if __ANDROID__
        public override Task RemoveTag(string tag)
#else
        public Task RemoveTag(string tag)
#endif
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
            tags.Remove(tag);
            return this.SetTags(tags.ToArray());
        }

#if __ANDROID__
        public override Task ClearTags() => this.SetTags(null);
#else
        public Task ClearTags() => this.SetTags(null);
#endif



        protected async Task SetTags(params string[]? tags)
        {
            if (this.InstallationId == null)
                return;

            var install = await this.hub.GetInstallationAsync(this.InstallationId);
            install.Tags = tags?.ToList() ?? new List<string>(0);
            await this.hub.CreateOrUpdateInstallationAsync(install);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.RegisteredTags = tags;
        }
    }
}
#endif