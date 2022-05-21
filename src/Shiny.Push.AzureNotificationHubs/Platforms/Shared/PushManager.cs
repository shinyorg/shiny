using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;

namespace Shiny.Push.AzureNotificationHubs;


public class PushManager : IPushManager,
                           IPushTagSupport,
                           IShinyStartupTask
{
    readonly INativeAdapter native;
    readonly ILogger logger;
    readonly PushContainer container;
    readonly AzureNotificationConfig config;


    public PushManager(
        INativeAdapter native,
        ILogger<PushManager> logger,
        PushContainer container,
        AzureNotificationConfig config
    )
    {
        this.native = native;
        this.container = container;
        this.logger = logger;
        this.config = config;
    }


    public async void Start()
    {
        // this only runs on Android/Firebase
        this.native.OnTokenRefreshed = async token =>
        {
            this.container.SetCurrentToken(token, false);
            try
            {
                // if this fails, we should have a backup plan like a job
                await this.Update(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to register new native push token with Azure Notification Hubs", ex);
            }
        };

        this.native.OnReceived = push => this.container.OnReceived(push);
        this.native.OnEntry = push => this.container.OnEntry(push);
        await this.container
            .TryAutoStart(this.native, this.logger)
            .ConfigureAwait(false);
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
        var access = await this.native
            .RequestAccess()
            .ConfigureAwait(false);
        this.logger.LogInformation($"OS Permission: {access.Status} - Native Token: {access.RegistrationToken}");

        if (access.Status == AccessState.Available)
        {
            this.InstallationId ??= Guid.NewGuid().ToString().Replace("-", "");
            await this
                .Update(access.RegistrationToken!)
                .ConfigureAwait(false);

            // this is to ensure that azure has enough time to propogate the installation ID
            await Task
                .Delay(this.config.AzureAuthenticationWaitTimeMs)
                .ConfigureAwait(false);

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
                await this.CreateClient()
                    .DeleteInstallationAsync(this.InstallationId)
                    .ConfigureAwait(false);
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

        var hub = this.CreateClient();
        var install = this.CreateInstall(this.container.CurrentRegistrationToken!);
        if (tags == null || tags.Length == 0)
            install.Tags = null;
        else
            install.Tags = tags.ToList();

        await hub
            .CreateOrUpdateInstallationAsync(install)
            .ConfigureAwait(false);

        this.container.CurrentRegistrationTokenDate = DateTime.UtcNow;
        this.container.RegisteredTags = tags;
    }


    protected async Task Update(string nativeRegToken, CancellationToken cancelToken = default)
    {
        var install = this.CreateInstall(nativeRegToken);
        this.logger.LogInformation($"ANH Token: {this.InstallationId}");

        await this.CreateClient()
            .CreateOrUpdateInstallationAsync(install, cancelToken)
            .ConfigureAwait(false);
        this.container.SetCurrentToken(nativeRegToken, false);
    }


    protected Installation CreateInstall(string nativeRegToken)
    {
        DateTime? date = null;
        if (this.config.ExpirationTime != null)
            date = DateTime.Now.Add(this.config.ExpirationTime.Value);

        return new Installation
        {
            InstallationId = this.InstallationId,
            PushChannel = nativeRegToken,
            ExpirationTime = date,
            Tags = this.RegisteredTags?.ToList(),
#if __IOS__
            Platform = NotificationPlatform.Apns
#elif __ANDROID__
            Platform = NotificationPlatform.Fcm
#elif WINDOWS_UWP
            Platform = NotificationPlatform.Wns
#endif
        };
    }


    protected NotificationHubClient CreateClient() => new NotificationHubClient(
        this.config.ListenerConnectionString,
        this.config.HubName
    );
}