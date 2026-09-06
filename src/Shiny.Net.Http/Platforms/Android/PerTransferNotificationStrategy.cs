using System;
using System.Collections.Generic;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


[Obsolete(
    "Use services.AddTransferProgress() instead. This strategy posts a SECOND notification alongside the " +
    "transfer foreground service's own, so the user sees both a progress notification and a redundant " +
    "'Shiny service is continuing to transfer data in the background'. TransferProgressManager draws on " +
    "the foreground service's notification instead, and shares its wording with the iOS Live Activity."
)]
public class PerTransferNotificationStrategy : AbstractTransferNotificationStrategy
{
    static int CurrentNotificationId = 9999;
    readonly Dictionary<string, (int NotificationId, NotificationCompat.Builder Builder)> notificationCfg = new();
    EventHandler<HttpTransferResult>? updateHandler;

    public PerTransferNotificationStrategy(
        IHttpTransferManager manager,
        AndroidPlatform platform,
        ILogger<PerTransferNotificationStrategy> logger
    ) : base(platform, logger)
    {
        this.Manager = manager;
    }


    protected IHttpTransferManager Manager { get; }


    public override void Start()
    {
        var channelId = this.CreateChannel();

        this.updateHandler = (_, transfer) =>
        {
            if (!this.notificationCfg.ContainsKey(transfer.Request.Identifier))
            {
                var id = ++CurrentNotificationId;
                this.Logger.LogDebug($"Starting new notification '{id}' for transfer '{transfer.Request.Identifier}'");
                var builder = this.CreateBuilder(channelId);
                this.notificationCfg.Add(transfer.Request.Identifier, (id, builder));
            }

            var settings = this.notificationCfg[transfer.Request.Identifier];
            if (transfer.Status == HttpTransferState.InProgress)
            {
                this.Customize(settings.Builder, transfer);
                this.NotificationManager.Notify(settings.NotificationId, settings.Builder.Build());
            }
            else
            {
                this.NotificationManager.Cancel(settings.NotificationId);
                this.notificationCfg.Remove(transfer.Request.Identifier);
            }
        };
        this.Manager.UpdateReceived += this.updateHandler;
    }

    public void Stop()
    {
        if (this.updateHandler != null)
        {
            this.Manager.UpdateReceived -= this.updateHandler;
            this.updateHandler = null;
        }
    }


    protected virtual void Customize(NotificationCompat.Builder builder, HttpTransferResult result)
    {
        this.Logger.LogDebug("Updating Foreground Notification");
        var percentComplete = result.IsDeterministic ? Convert.ToInt32(result.Progress.PercentComplete * 100) : 0;

        var transferTxt = result.Request.Type.IsUpload() ? "Uploading to" : "Downloading from";
        builder
            .SetContentTitle("File Transfers")
            //.SetContentInfo("")
            //.SetSubText("")
            .SetContentText($"Processing Background Transfer - {transferTxt} {result.Request.Uri}")
            .SetProgress(
                100,
                percentComplete,
                !result.IsDeterministic
            );

        //result.Progress.EstimatedTimeRemaining;
        //result.Progress.BytesPerSecond
    }
}