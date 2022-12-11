using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Logging;

namespace Shiny.Push;


public class AzureNotificationHubsPushProvider //: IPushProvider, IPushTagSupport
{
}

//public async void Start()
//{
//    this.native.OnReceived = push => this.container.OnReceived(push);
//    this.native.OnEntry = push => this.container.OnEntry(push);

//    if (!this.CurrentRegistrationToken.IsEmpty())
//    {
//        try
//        {
//            // TODO: if native token changes, we need to update anh or kill the token?
//            var result = await this.native.RequestAccess();

//            if (result.Status == AccessState.Available && !result.RegistrationToken!.Equals(this.NativeToken))
//            {
//                // token is not changing on anh, so we don't need to fire events
//                await this.Update(result.RegistrationToken!).ConfigureAwait(false);
//            }
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError("Failed to register new native push token with Azure Notification Hubs", ex);
//            //await this.UnRegister(); // TODO:??
//        }
//    }
//}


//public IObservable<PushNotification> WhenReceived() => this.container.WhenReceived();
//public string? CurrentRegistrationToken => this.InstallationId;
//public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
//public string[]? RegisteredTags => this.container.RegisteredTags;
//public string? InstallationId
//{
//    get => this.container.Store.Get<string>(nameof(this.InstallationId));
//    private set => this.container.Store.SetOrRemove(nameof(this.InstallationId), value);
//}

//string NativeToken
//{
//    get => this.container.Store.Get<string>(nameof(this.NativeToken));
//    set => this.container.Store.SetOrRemove(nameof(this.NativeToken), value);
//}

//public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
//{
//    var access = await this.native
//        .RequestAccess()
//        .ConfigureAwait(false);
//    this.logger.LogInformation($"OS Permission: {access.Status} - Native Token: {access.RegistrationToken}");

//    if (access.Status == AccessState.Available)
//    {
//        this.InstallationId ??= Guid.NewGuid().ToString().Replace("-", "");
//        this.NativeToken = access.RegistrationToken!;

//        await this
//            .Update(access.RegistrationToken!)
//            .ConfigureAwait(false);

//        // this is to ensure that azure has enough time to propogate the installation ID
//        await Task
//            .Delay(this.config.AzureAuthenticationWaitTimeMs)
//            .ConfigureAwait(false);

//        access = new PushAccessState(AccessState.Available, this.InstallationId);
//    }
//    return access;
//}


//public async Task UnRegister()
//{
//    if (this.InstallationId != null)
//    {
//        try
//        {
//            await this.CreateClient()
//                .DeleteInstallationAsync(this.InstallationId)
//                .ConfigureAwait(false);
//        }
//        catch (MessagingEntityNotFoundException)
//        {
//            // who cares ??
//            // retry??  does it matter?  native token is killed
//        }
//        this.InstallationId = null;
//    }
//    await this.native.UnRegister();
//    this.container.ClearRegistration();
//}


//public Task AddTag(string tag)
//{
//    var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
//    tags.Add(tag);
//    return this.SetTags(tags.ToArray());
//}


//public Task RemoveTag(string tag)
//{
//    var tags = this.RegisteredTags?.ToList() ?? new List<string>(0);
//    tags.Remove(tag);
//    return this.SetTags(tags.ToArray());
//}


//public Task ClearTags() => this.SetTags(null);


//public async Task SetTags(params string[]? tags)
//{
//    if (this.InstallationId == null)
//        return;

//    var hub = this.CreateClient();
//    var install = this.CreateInstall(this.container.CurrentRegistrationToken!);
//    if (tags == null || tags.Length == 0)
//        install.Tags = null;
//    else
//        install.Tags = tags.ToList();

//    await hub
//        .CreateOrUpdateInstallationAsync(install)
//        .ConfigureAwait(false);

//    this.container.CurrentRegistrationTokenDate = DateTime.UtcNow;
//    this.container.RegisteredTags = tags;
//}


//protected async Task Update(string nativeRegToken, CancellationToken cancelToken = default)
//{
//    var install = this.CreateInstall(nativeRegToken);
//    this.logger.LogInformation($"ANH Token: {this.InstallationId}");

//    await this.CreateClient()
//        .CreateOrUpdateInstallationAsync(install, cancelToken)
//        .ConfigureAwait(false);
//    this.container.SetCurrentToken(nativeRegToken, false);
//}


//protected Installation CreateInstall(string nativeRegToken)
//{
//    DateTime? date = null;
//    if (this.config.ExpirationTime != null)
//        date = DateTime.Now.Add(this.config.ExpirationTime.Value);

//    return new Installation
//    {
//        InstallationId = this.InstallationId,
//        PushChannel = nativeRegToken,
//        ExpirationTime = date,
//        Tags = this.RegisteredTags?.ToList(),
//#if APPLE
//            Platform = NotificationPlatform.Apns
//#elif ANDROID
//            Platform = NotificationPlatform.Fcm
//#endif
//    };
//}


//protected NotificationHubClient CreateClient() => new NotificationHubClient(
//    this.config.ListenerConnectionString,
//    this.config.HubName
//);