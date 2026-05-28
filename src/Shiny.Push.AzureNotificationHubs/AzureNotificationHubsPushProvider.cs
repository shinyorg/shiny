#if PLATFORM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
#if APPLE
using Foundation;
#endif

namespace Shiny.Push;


public class AzureNotificationHubsPushProvider(
    AzureNotificationConfig config,
    IServiceProvider services,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<AzureNotificationHubsPushProvider> logger
) : IPushProvider, IPushTagSupport
{
    readonly NotificationHubClient client = new(config.ListenerConnectionString, config.HubName);

    static string Key(string prop) => $"Shiny.Push.AzureNotificationHubsPushProvider.{prop}";


    string? nativeToken = store.Get<string>(Key(nameof(NativeToken)));
    public string? NativeToken
    {
        get => this.nativeToken;
        set
        {
            if (this.nativeToken != value)
            {
                this.nativeToken = value;
                store.SetOrRemove(Key(nameof(NativeToken)), value);
            }
        }
    }


    string? installationId = store.Get<string>(Key(nameof(InstallationId)));
    public string? InstallationId
    {
        get => this.installationId;
        set
        {
            if (this.installationId != value)
            {
                this.installationId = value;
                store.SetOrRemove(Key(nameof(InstallationId)), value);
            }
        }
    }


    string[]? registeredTags = store.Get<string[]>(Key(nameof(RegisteredTags)));
    public string[]? RegisteredTags
    {
        get => this.registeredTags;
        set
        {
            if (this.registeredTags != value)
            {
                this.registeredTags = value;
                store.SetOrRemove(Key(nameof(RegisteredTags)), value);
            }
        }
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


#if IOS || MACCATALYST
    public async Task<string> Register(NSData rawToken)
    {
        var nativeToken = rawToken.ToPushTokenString();
#endif
#if ANDROID || WINDOWS
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
            .Delay(config.AzureAuthenticationWaitTimeMs)
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
        if (config.ExpirationTime != null)
            date = DateTime.Now.Add(config.ExpirationTime.Value);

        return new Installation
        {
            InstallationId = this.InstallationId,
            PushChannel = nativeRegToken,
            ExpirationTime = date,
            Tags = this.RegisteredTags?.ToList(),
#if APPLE
            Platform = NotificationPlatform.Apns
#elif ANDROID
            Platform = NotificationPlatform.FcmV1
#elif WINDOWS
            Platform = NotificationPlatform.Wns
#endif
        };
    }
    

    protected async Task Update(string nativeRegToken, CancellationToken cancelToken = default)
    {
        var install = this.GetInstallation(nativeRegToken);
        logger.LogInformation($"ANH Token: {this.InstallationId}");

        await this.RunInstallEvents(install).ConfigureAwait(false);
        await this.client
            .CreateOrUpdateInstallationAsync(install, cancelToken)
            .ConfigureAwait(false);
    }

    
    protected async Task RunInstallEvents(Installation installation)
    {
        if (config.BeforeSendInstallation != null)
        {
            try
            {
                await config
                    .BeforeSendInstallation(installation)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run AzureNotificationConfig.BeforeSendInstallation");
            }
        }

        await services
            .RunDelegates<IPushInstallationEvent>(x => x.OnBeforeSend(installation), logger)
            .ConfigureAwait(false);
    }
}
#endif