using Android.App;
using Android.Content;
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
        var channelId = not.ChannelId ?? this.Config.DefaultChannel?.Id;
        var builder = new NotificationCompat.Builder(this.Platform.AppContext, channelId!)!;

        if (not != null)
        {
            // TODO: config OR click_action
            var intent = new Intent(not.ClickAction ?? ShinyPushIntents.NotificationClickAction);
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
}