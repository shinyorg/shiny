#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Shiny.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        readonly NotificationHubClient hub;


#if __ANDROID__
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
        public override void Start()
        {
            // wireup firebase if it was active
            if (this.CurrentRegistrationToken != null)
                Firebase.Messaging.FirebaseMessaging.Instance.AutoInitEnabled = true;

            // don't fire the base or the firebase start will overwrite the current
            // registration token with the firebase token, not the AZH installationID
            ShinyFirebaseService.NewToken = async token =>
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
            };

            ShinyFirebaseService.MessageReceived = async message =>
            {
                var pr = this.FromNative(message);
                await this.OnPushReceived(pr);
            };
        }
#endif

        string? InstallationId
        {
            get => this.Settings.Get<string>(nameof(this.InstallationId));
            set => this.Settings.SetOrRemove(nameof(this.InstallationId), value);
        }


        string? NativeRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(NativeRegistrationToken));
            set => this.Settings.SetOrRemove(nameof(NativeRegistrationToken), value);
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await base.RequestAccess(cancelToken);

            if (access.Status == AccessState.Available)
            {
                this.NativeRegistrationToken = access.RegistrationToken;
                this.InstallationId = Guid.NewGuid().ToString().Replace("-", "");

                var install = new Installation
                {
                    InstallationId = this.InstallationId,
                    PushChannel = this.NativeRegistrationToken,
#if WINDOWS_UWP
                    Platform = NotificationPlatform.Wns
#elif __IOS__
                    Platform = NotificationPlatform.Apns
#elif __ANDROID__
                    Platform = NotificationPlatform.Fcm
#endif
                };
                await this.hub.CreateOrUpdateInstallationAsync(install, cancelToken);
                this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                this.CurrentRegistrationToken = this.InstallationId;

                access = new PushAccessState(AccessState.Available, this.InstallationId);
            }
            return access;
        }


        public override async Task UnRegister()
        {
            if (this.InstallationId != null)
            {
                try
                {
                    await this.hub.DeleteInstallationAsync(this.InstallationId);
                }
                catch (MessagingEntityNotFoundException)
                {
                    // who cares - it was already unregistered somehow
                }
                this.InstallationId = null;
            }
            this.NativeRegistrationToken = null;
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

#if __ANDROID__
        public override async Task SetTags(params string[]? tags)
#else
        public async Task SetTags(params string[]? tags)
#endif
        {
            if (this.InstallationId == null)
                return;

            var install = await this.hub.GetInstallationAsync(this.InstallationId);
            if (tags == null || tags.Length == 0)
                install.Tags = null;
            else
                install.Tags = tags.ToList();

            await this.hub.CreateOrUpdateInstallationAsync(install);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.RegisteredTags = tags;
        }
    }
}
#endif