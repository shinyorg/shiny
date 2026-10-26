using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Firebase.Messaging;

namespace Shiny.Push;


public record AndroidPushNotification(
    Notification? Notification,
    RemoteMessage NativeMessage,
    FirebaseConfig Config,
    AndroidPlatform Platform
) : PushNotification(NativeMessage.Data, Notification)
{

    public NotificationCompat.Builder CreateBuilder()
    {
        var not = this.NativeMessage.GetNotification();
        var channelId = not?.ChannelId ?? this.Config.DefaultChannel?.Id;
        var builder = new NotificationCompat.Builder(this.Platform.AppContext, channelId!)!;

        if (not != null)
        {
            var intent = new Intent(not.ClickAction ?? ShinyPushIntents.NotificationClickAction);
            foreach (var item in this.NativeMessage.Data)
                intent.PutExtra(item.Key, item.Value);

            var pendingIntent = PendingIntent.GetActivity(
                this.Platform.AppContext,
                99,
                intent,
                PendingIntentFlags.OneShot | PendingIntentFlags.Immutable
            );
            
            builder
                .SetContentTitle(not.Title)
                .SetContentIntent(pendingIntent);
            
            if (!not.Body.IsEmpty())
                builder.SetContentText(not.Body);

            if (!not.Ticker.IsEmpty())
                builder.SetTicker(not.Ticker);

            if (not.Icon.IsEmpty())
            {
                var id = this.Platform.GetDrawableByName("notification");
                if (id > 0)
                {
                    builder.SetSmallIcon(id);
                }
                else if (this.Platform.AppContext.ApplicationInfo!.Icon > 0)
                {
                    builder.SetSmallIcon(this.Platform.AppContext.ApplicationInfo!.Icon);
                }
            }
            else
            {
                var drawableId = this.Platform.GetDrawableByName(not.Icon);
                builder.SetSmallIcon(drawableId);
            }

            if (!not.Color.IsEmpty())
            {
                var colorId = this.Platform
                    .AppContext
                    .Resources!
                    .GetIdentifier(
                        not.Color,
                        "color",
                        this.Platform.AppContext.PackageName
                    );

                builder.SetColor(colorId);
            }
        }
        return builder;
    }


    /// <summary>
    /// The push image URL - FCM's notification image, otherwise the first FirebaseConfig.ImageKeys (default
    /// <see cref="PushImages.DefaultKeys"/>) data value that is an http(s) URL. Also set for data-only messages.
    /// </summary>
    public string? ImageUri => this.Notification?.ImageUri ?? PushImages.Find(this.NativeMessage.Data, this.Config.ImageKeys);


    /// <summary>
    /// Downloads <see cref="ImageUri"/> as a bitmap scaled down for a notification. Null when there is no image or it
    /// can't be downloaded or decoded - a notification should still go out without it.
    /// </summary>
    public async Task<Bitmap?> LoadImageAsync(CancellationToken cancelToken = default)
    {
        var uri = this.ImageUri;
        if (uri == null)
            return null;

        try
        {
            return await PushImageLoader.TryLoad(uri, cancelToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancelToken.IsCancellationRequested)
        {
            return null;
        }
    }


    /// <summary>
    /// <see cref="CreateBuilder"/> plus the push image (when there is one) as a BigPictureStyle and large icon.
    /// </summary>
    public async Task<NotificationCompat.Builder> CreateBuilderAsync(CancellationToken cancelToken = default)
    {
        var builder = this.CreateBuilder();
        var bitmap = await this.LoadImageAsync(cancelToken).ConfigureAwait(false);
        if (bitmap != null)
        {
            // the large icon shows while collapsed and gives way to the picture once expanded
            var style = new NotificationCompat.BigPictureStyle();
            style.BigPicture(bitmap);
            style.BigLargeIcon((Bitmap?)null);
            builder.SetLargeIcon(bitmap);
            builder.SetStyle(style);
        }
        return builder;
    }


    public void Notify(int notificationId, NotificationCompat.Builder builder)
    {
        using var service = this.Platform.GetSystemService<NotificationManager>(Context.NotificationService);
        service.Notify(notificationId, builder.Build());
    }


    /// <summary>
    /// This will create the default builder and send it
    /// </summary>
    public void SendDefault(int notificationId)
    {
        var builder = this.CreateBuilder();
        this.Notify(notificationId, builder);
    }


    /// <summary>
    /// Builds the default notification with the push image (<see cref="CreateBuilderAsync"/>) and sends it
    /// </summary>
    public async Task SendDefaultAsync(int notificationId, CancellationToken cancelToken = default)
    {
        var builder = await this.CreateBuilderAsync(cancelToken).ConfigureAwait(false);
        this.Notify(notificationId, builder);
    }
}
