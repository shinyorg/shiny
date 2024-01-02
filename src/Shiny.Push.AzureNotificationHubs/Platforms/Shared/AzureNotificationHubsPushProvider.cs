using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Logging;
#if APPLE
using Foundation;
#endif

namespace Shiny.Push;


public class AzureNotificationHubsPushProvider : NotifyPropertyChanged, IPushProvider, IPushTagSupport
{
    readonly AzureNotificationConfig config;
    readonly ILogger logger;
    readonly NotificationHubClient client;


    public AzureNotificationHubsPushProvider(
        AzureNotificationConfig config,
        ILogger<AzureNotificationHubsPushProvider> logger
    )
    {
        this.config = config;
        this.logger = logger;
        this.client = new NotificationHubClient(
            config.ListenerConnectionString,
            config.HubName
        );
    }


    string? nativeToken;
    public string? NativeToken
    {
        get => this.nativeToken;
        set => this.Set(ref this.nativeToken, value);
    }


    string? installationId;
    public string? InstallationId
    {
        get => this.installationId;
        set => this.Set(ref this.installationId, value);
    }


    string[]? registeredTags;
    public string[]? RegisteredTags
    {
        get => this.registeredTags;
        set => this.Set(ref this.registeredTags, value);
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
        // TODO: crash/assert
        //if (this.InstallationId == null)
        //    return;

        var install = this.GetInstallation(this.NativeToken!);
        if (tags == null || tags.Length == 0)
            install.Tags = null;
        else
            install.Tags = tags.ToList();

        await this.client
            .CreateOrUpdateInstallationAsync(install)
            .ConfigureAwait(false);

        this.RegisteredTags = tags;
    }


#if APPLE
    public async Task<string> Register(NSData rawToken)
    {
        var nativeToken = rawToken.ToPushTokenString();
#endif
#if ANDROID
    public async Task<string> Register(string nativeToken)
    { 
#endif
        this.InstallationId ??= Guid.NewGuid().ToString().Replace("-", "");
        
        await this
            .Update(nativeToken)
            .ConfigureAwait(false);

        this.NativeToken = nativeToken!;

        // this is to ensure that azure has enough time to propogate the installation ID
        await Task
            .Delay(this.config.AzureAuthenticationWaitTimeMs)
            .ConfigureAwait(false);

        return this.InstallationId;
    }


    public async Task UnRegister()
    {
        if (this.InstallationId == null)
            return;

        try
        {
            await this.client
                .DeleteInstallationAsync(this.InstallationId)
                .ConfigureAwait(false);
        }
        catch (MessagingEntityNotFoundException)
        {
            // who cares ??
            // retry??  does it matter?  native token is killed
        }
        this.InstallationId = null;
        this.NativeToken = null;
        this.RegisteredTags = null;
    }


    protected Installation GetInstallation(string nativeRegToken)
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
#if APPLE
            Platform = NotificationPlatform.Apns
#elif ANDROID
            Platform = NotificationPlatform.Fcm
#endif
        };
    }

    protected async Task Update(string nativeRegToken, CancellationToken cancelToken = default)
    {
        var install = this.GetInstallation(nativeRegToken);
        this.logger.LogInformation($"ANH Token: {this.InstallationId}");

        await this.client
            .CreateOrUpdateInstallationAsync(install, cancelToken)
            .ConfigureAwait(false);
    }
}