using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.Core;
using Microsoft.Extensions.Logging;

namespace Shiny.Push.FirebaseMessaging;


public class PushManager : IPushManager, IPushTagSupport
{
    readonly NativeAdapter adapter;
    readonly PushContainer container;
    readonly ILogger logger;
    readonly FirebaseConfiguration config;


    public PushManager(
        NativeAdapter adapter,
        PushContainer container,
        ILogger<PushManager> logger,
        FirebaseConfiguration config
    )
    {
        config.AssertValid();

        this.adapter = adapter;
        this.container = container;
        this.logger = logger;
        this.config = config;
    }


    public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
    public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
    public string[]? RegisteredTags => this.container.RegisteredTags;
    public IObservable<PushNotification> WhenReceived() => this.container.WhenReceived();


    public async void Start()
    {
        if (this.CurrentRegistrationToken != null)
        {
            try
            {
                this.TryStartFirebase();
                var fcmToken = await this.GetFcmToken().ConfigureAwait(false);
                this.container.SetCurrentToken(fcmToken, true);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning("Failed to start Firebase Push", ex);
            }
        }
    }


    protected virtual void TryStartFirebase()
    {
        if (App.DefaultInstance == null)
        {
            if (this.config.UseEmbeddedConfiguration)
            {
                App.Configure();
                if (Messaging.SharedInstance == null)
                    throw new ArgumentException("Failed to configure firebase messaging - ensure you have GoogleService-Info.plist included in your iOS project and that it is set to a BundleResource");

                Messaging.SharedInstance!.AutoInitEnabled = true;
            }
            else
            {
                App.Configure(new Options(
                    this.config.AppId!,
                    this.config.SenderId!
                ) {
                    ApiKey = this.config.ApiKey,
                    ProjectId = this.config.ProjectId
                });
            }
        }
    }


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        var fcmToken = await this.GetFcmToken().ConfigureAwait(false);
        this.container.SetCurrentToken(fcmToken, false);
        return new PushAccessState(AccessState.Available, fcmToken);
    }


    public async Task UnRegister()
    {
        this.container.ClearRegistration();
        await Messaging.SharedInstance.DeleteTokenAsync();
        await this.adapter.UnRegister().ConfigureAwait(false);
    }


    public async Task AddTag(string tag)
    {
        var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
        tags.Add(tag);

        await Messaging.SharedInstance.SubscribeAsync(tag);
        this.container.RegisteredTags = tags.ToArray();
    }


    public async Task RemoveTag(string tag)
    {
        await Messaging
            .SharedInstance
            .UnsubscribeAsync(tag)
            .ConfigureAwait(false);

        if (this.RegisteredTags != null)
        {
            var tags = this.RegisteredTags.ToList();
            if (tags.Remove(tag))
                this.container.RegisteredTags = tags.ToArray();
        }
    }


    public async Task ClearTags()
    {
        if (this.RegisteredTags != null)
        {
            foreach (var tag in this.RegisteredTags)
            {
                await Messaging
                    .SharedInstance
                    .UnsubscribeAsync(tag)
                    .ConfigureAwait(false);
            }
        }
        this.container.RegisteredTags = null;
    }


    public async Task SetTags(params string[]? tags)
    {
        await this.ClearTags().ConfigureAwait(false);
        if (tags != null)
        {
            foreach (var tag in tags)
                await this.AddTag(tag).ConfigureAwait(false);
        }
    }


    protected async Task<string> GetFcmToken()
    {
        var rawToken = await this.adapter.RequestRawToken().ConfigureAwait(false);
        this.TryStartFirebase();

        Messaging.SharedInstance.ApnsToken = rawToken;
        var fcmToken = await Messaging.SharedInstance.FetchTokenAsync();

        if (fcmToken == null)
            throw new InvalidOperationException("FCM Token is null");

        return fcmToken;
    }
}
