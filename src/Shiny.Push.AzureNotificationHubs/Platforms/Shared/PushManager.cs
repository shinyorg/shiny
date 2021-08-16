#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push.AzureNotificationHubs
{
    public partial class PushManager : IPushManager,
                                       IPushTagSupport,
                                       IShinyStartupTask
    {
        readonly INativeAdapter native;
        readonly ILogger logger;
        readonly PushContainer container;
        readonly NotificationHubClient hub;


        protected PushManager(INativeAdapter native,
                              ILogger<PushManager> logger,
                              PushContainer container,
                              AzureNotificationConfig config)
        {
            this.native = native;
            this.container = container;
            this.logger = logger;
            this.hub = new NotificationHubClient(
                config.ListenerConnectionString,
                config.HubName
            );
        }


        public void Start()
        {
            // this only runs on Android/Firebase
            this.native.OnTokenRefreshed = async token =>
            {
                // TODO: update installation - if installation update fails - we'll need to try again somehow (job?)
                //this.container.SetCurrentToken(token, false);
                //await this.container.OnTokenRefreshed(token).ConfigureAwait(false);
            };

            this.native.OnReceived = async push =>
            {
                await this.container.OnReceived(push).ConfigureAwait(false);
#if __ANDROID__
                //if (push.Notification != null)
                //    await this.notificationManager.Send(push.Notification);
#endif
            };

            this.native.OnEntry = push => this.container.OnEntry(push);

            //await this.native
            //    .TryAutoStart(this.adapter, this.logger)
            //    .ConfigureAwait(false);
        }


        public IObservable<PushNotification> WhenReceived() => this.container.WhenReceived();
        public string? CurrentRegistrationToken => this.InstallationId;
        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string[]? RegisteredTags => this.container.RegisteredTags;
        public string? InstallationId
        {
            get => this.container.Store.Get<string>(nameof(this.InstallationId));
            private set => this.container.Store.SetOrRemove(nameof(this.InstallationId), value);
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var access = await this.native.RequestAccess().ConfigureAwait(false);
            this.logger.LogInformation($"OS Permission: {access.Status} - Native Token: {access.RegistrationToken}");

            if (access.Status == AccessState.Available)
            {
                this.InstallationId ??= Guid.NewGuid().ToString().Replace("-", "");

                var install = new Installation
                {
                    InstallationId = this.InstallationId,
                    PushChannel = access.RegistrationToken,
#if __IOS__
                    Platform = NotificationPlatform.Apns
#elif __ANDROID__
                    Platform = NotificationPlatform.Fcm
#elif WINDOWS_UWP
                    Platform = NotificationPlatform.Wns
#endif
                };
                this.logger.LogInformation($"ANH Token: {this.InstallationId}");

                await this.hub.CreateOrUpdateInstallationAsync(install, cancelToken).ConfigureAwait(false);
                this.container.SetCurrentToken(access.RegistrationToken!, false);

                access = new PushAccessState(AccessState.Available, this.InstallationId);
            }
            return access;
        }


        public async Task UnRegister()
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
            await this.native.UnRegister();
            this.container.ClearRegistration();
        }


        public Task AddTag(string tag)
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
            tags.Add(tag);
            return this.SetTags(tags.ToArray());
        }


        public Task RemoveTag(string tag)
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
            tags.Remove(tag);
            return this.SetTags(tags.ToArray());
        }


        public Task ClearTags() => this.SetTags(null);


        public async Task SetTags(params string[]? tags)
        {
            if (this.InstallationId == null)
                return;

            var install = await this.hub.GetInstallationAsync(this.InstallationId).ConfigureAwait(false);
            if (tags == null || tags.Length == 0)
                install.Tags = null;
            else
                install.Tags = tags.ToList();

            await this.hub.CreateOrUpdateInstallationAsync(install).ConfigureAwait(false);
            this.container.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.container.RegisteredTags = tags;
        }
    }
}
#endif