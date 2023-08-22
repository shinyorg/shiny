using Android.App;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public abstract class AbstractTransferNotificationStrategy : IShinyStartupTask
{
    protected AbstractTransferNotificationStrategy(AndroidPlatform platform, ILogger logger)
    {
        this.Platform = platform;
        this.Logger = logger;

        this.NotificationManager = NotificationManagerCompat.From(this.Platform.AppContext);
    }


    public abstract void Start();

    protected AndroidPlatform Platform { get; }
    protected ILogger Logger { get; }
    protected NotificationManagerCompat NotificationManager { get; }
    protected string NotificationChannelId { get; set; } = "Transfers";


    protected virtual NotificationCompat.Builder CreateBuilder(string channelId)
    {
        var build = new NotificationCompat.Builder(this.Platform.AppContext, channelId)
            .SetSmallIcon(this.Platform.GetNotificationIconResource())
            .SetTicker("...")
            .SetContentTitle("Shiny HTTP Transfers")
            .SetContentText("Shiny service is continuing to transfer data in the background");

        return build;
    }

    protected virtual string CreateChannel()
    {
        if (this.NotificationManager!.GetNotificationChannel(this.NotificationChannelId) != null)
            return this.NotificationChannelId;

        var channel = new NotificationChannel(
            this.NotificationChannelId,
            this.NotificationChannelId,
            NotificationImportance.Low
        );
        channel.SetShowBadge(false);
        this.NotificationManager.CreateNotificationChannel(channel);
        return this.NotificationChannelId;
    }
}

