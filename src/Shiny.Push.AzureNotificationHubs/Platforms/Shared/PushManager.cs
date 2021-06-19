#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public class PushManager : Shiny.Push.PushManager, IPushTagSupport
    {
        readonly NotificationHubClient hub;
        readonly ILogger logger;


#if __ANDROID__
        public PushManager(AzureNotificationConfig config,
                           ShinyCoreServices services,
                           ILogger<IPushManager> logger,
                           Shiny.Notifications.INotificationManager notifications)
                           : base(services, notifications, logger)
#elif WINDOWS_UWP
        public PushManager(AzureNotificationConfig config,
                           ShinyCoreServices services,
                           ILogger<IPushManager> logger) : base(services, logger)
#else
        public PushManager(AzureNotificationConfig config,
                           ShinyCoreServices services,
                           ILogger<IPushManager> logger) : base(services)
#endif
        {
            this.logger = logger;
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
                        var install = await this.hub.GetInstallationAsync(this.InstallationId).ConfigureAwait(false);
                        install.PushChannel = token;
                        await this.hub.CreateOrUpdateInstallationAsync(install).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    // should I unreg here?
                    this.logger.LogError(ex, "There was an registering the new firebase token to Azure Notification Hub");
                }
            };

            ShinyFirebaseService.MessageReceived = async message =>
            {
                try
                {
                    var pr = this.FromNative(message);
                    await this.OnPushReceived(pr).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "There was an error processing received message");
                }
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
            var access = await base.RequestAccess(cancelToken).ConfigureAwait(false);
            this.logger.LogInformation($"OS Permission: " + access.Status);

            if (access.Status == AccessState.Available)
            {
                this.InstallationId ??= Guid.NewGuid().ToString().Replace("-", "");

#if __IOS__
                this.NativeRegistrationToken = await this.GetPushChannel(cancelToken).ConfigureAwait(false);
                //this.NativeRegistrationToken = rawToken.ToArray().ToHex();
#else
                this.NativeRegistrationToken = access.RegistrationToken;
#endif
                this.logger.LogInformation("Native Notification Token: " + this.NativeRegistrationToken);

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
                await this.hub.CreateOrUpdateInstallationAsync(install, cancelToken).ConfigureAwait(false);
                this.logger.LogInformation("Azure Notification Hub InstallationID: " + this.InstallationId);

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
                    await this.hub.DeleteInstallationAsync(this.InstallationId).ConfigureAwait(false);
                }
                catch (MessagingEntityNotFoundException)
                {
                    // who cares - it was already unregistered somehow
                }
                this.InstallationId = null;
            }
            this.NativeRegistrationToken = null;
            await base.UnRegister().ConfigureAwait(false);
        }


#if __IOS__

        protected virtual async Task<string> GetPushChannel(CancellationToken cancelToken)
        {
            var deviceToken = await this.RequestDeviceToken(cancelToken).ConfigureAwait(false);
            var token = System.Text.Encoding.UTF8.GetString(deviceToken.ToArray());
            return token;
        }
#endif


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

            var install = await this.hub.GetInstallationAsync(this.InstallationId).ConfigureAwait(false);
            if (tags == null || tags.Length == 0)
                install.Tags = null;
            else
                install.Tags = tags.ToList();

            await this.hub.CreateOrUpdateInstallationAsync(install).ConfigureAwait(false);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.RegisteredTags = tags;
        }
    }
}
#endif