using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Windows.Networking.PushNotifications;

namespace Shiny.Push;


public class PushManager(
    IServiceProvider services,
    IKeyValueStore store,
    ILogger<PushManager> logger,
    IPushProvider? provider = null
) : IPushManager
{
    readonly IServiceProvider services = services;
    readonly IKeyValueStore store = store;
    readonly ILogger logger = logger;
    readonly IPushProvider? provider = provider;
    PushNotificationChannel? channel;

    static string Key(string prop) => $"Shiny.Push.PushManager.{prop}";

    public IPushTagSupport? Tags => this.provider as IPushTagSupport;


    string? regToken = store.Get<string>(Key(nameof(RegistrationToken)));
    public string? RegistrationToken
    {
        get => this.regToken;
        set
        {
            if (this.regToken != value)
            {
                this.regToken = value;
                this.store.SetOrRemove(Key(nameof(RegistrationToken)), value);
            }
        }
    }


    string? nativeToken = store.Get<string>(Key(nameof(NativeRegistrationToken)));
    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set
        {
            if (this.nativeToken != value)
            {
                this.nativeToken = value;
                this.store.SetOrRemove(Key(nameof(NativeRegistrationToken)), value);
            }
        }
    }


    public Task<AccessState> GetCurrentAccess()
        => Task.FromResult(this.channel == null ? AccessState.Unknown : AccessState.Available);


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        try
        {
            this.channel = await PushNotificationChannelManager
                .GetDefault()
                .CreatePushNotificationChannelForApplicationAsync()
                .AsTask(cancelToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create push notification channel");
            return new PushAccessState(AccessState.Denied, null);
        }

        this.channel.PushNotificationReceived -= this.OnPushReceived;
        this.channel.PushNotificationReceived += this.OnPushReceived;

        var nativeToken = this.channel.Uri;
        var regToken = nativeToken;

        if (this.provider != null)
            regToken = await this.provider.Register(nativeToken).ConfigureAwait(false);

        if (this.RegistrationToken != regToken)
        {
            this.NativeRegistrationToken = nativeToken;
            this.RegistrationToken = regToken;

            await this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnNewToken(regToken),
                    this.logger
                )
                .ConfigureAwait(false);
        }

        return new PushAccessState(AccessState.Available, this.RegistrationToken);
    }


    public async Task UnRegister()
    {
        if (this.channel == null)
            return;

        var oldToken = this.RegistrationToken;
        this.channel.PushNotificationReceived -= this.OnPushReceived;
        this.channel.Close();
        this.channel = null;

        if (this.provider != null)
            await this.provider.UnRegister().ConfigureAwait(false);

        this.RegistrationToken = null;
        this.NativeRegistrationToken = null;

        if (oldToken != null)
        {
            await this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnUnRegistered(oldToken),
                    this.logger
                )
                .ConfigureAwait(false);
        }
    }


    void OnPushReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
    {
        try
        {
            var data = new Dictionary<string, string>();
            Notification? notification = null;
            string? content = null;

            switch (args.NotificationType)
            {
                case PushNotificationType.Toast:
                    content = args.ToastNotification?.Content?.GetXml();
                    notification = new Notification(null, content);
                    break;

                case PushNotificationType.Tile:
                    content = args.TileNotification?.Content?.GetXml();
                    break;

                case PushNotificationType.Badge:
                    content = args.BadgeNotification?.Content?.GetXml();
                    break;

                case PushNotificationType.Raw:
                    content = args.RawNotification?.Content;
                    break;
            }

            if (content != null)
                data["content"] = content;

            data["type"] = args.NotificationType.ToString();

            var push = new PushNotification(data, notification);
            this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnReceived(push),
                    this.logger
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error handling push notification");
        }
    }
}
