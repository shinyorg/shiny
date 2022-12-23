using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Shiny.Net.Http;


class TransferJob : IJob
{
    const int NOTIFICATION_ID = 222222;
    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IConnectivity connectivity;
    readonly IHttpTransferManager manager;
    readonly IEnumerable<IHttpTransferDelegate> delegates;
    readonly NotificationManagerCompat notifications;


    public TransferJob(
        ILogger<TransferJob> logger,
        AndroidPlatform platform,
        IHttpTransferManager manager,
        IConnectivity connectivity,
        IEnumerable<IHttpTransferDelegate> delegates
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.manager = manager;
        this.connectivity = connectivity;        
        this.delegates = delegates;
        this.notifications = NotificationManagerCompat.From(platform.AppContext);
    }



    // TODO: ensure job does not run multiple times
    // TODO: allow configurable amount of transfer at a time
    // TODO: check if progress is interdeterminate
    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        // TODO: always loop again if it has been several mins since the last full loop
        // TODO: reloop for new transfers, check for cancelled transfers
        // TODO: anything that is paused manually should NOT be looked at
        // TODO: anything that was paused due to server issue (retry) or network change, should be looped
        //while (this.manager.Transfers.Count > 0)
        //{
        //    // TODO: this may infinite loop, so we only jobs that can run
        //    var transfer = this.manager.Transfers.First();

        //    if (transfer.Request.UseMeteredConnection || !this.connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet))
        //    {
        //        await this.RunTransfer(transfer, cancelToken);
        //    }
        //}
    }


    // TODO: consider moving notifications into transfer as well
    async Task RunTransfer(IHttpTransfer transfer, CancellationToken cancelToken)
    {
        // a cancellation from the job likely means loss of internet
        using var _ = cancelToken.Register(() => this.manager.Pause(transfer.Identifier));
        IDisposable? sub = null;

        await this.manager.Resume(transfer.Identifier);
        var builder = this.StartNotification(transfer);
        if (builder != null)
            this.notifications.Notify(NOTIFICATION_ID, builder.Build());

        try
        {
            // wait for pause/complete/error/cancel then move to next transfer
            await transfer
                .ListenToMetrics()
                .Do(x =>
                {
                    if (x.Status == HttpTransferState.InProgress)
                    {
                        if (builder != null)
                        {
                            builder.SetProgress(100, Convert.ToInt32(x.PercentComplete * 100), false);
                            this.notifications.Notify(NOTIFICATION_ID, builder.Build());
                        }
                    }
                })
                .Where(x =>
                    x.Status != HttpTransferState.InProgress &&
                    x.Status != HttpTransferState.Pending
                )
                .Take(1)
                .ToTask(cancelToken)
                .ConfigureAwait(false);
        }
        finally
        {
            this.notifications.Cancel(NOTIFICATION_ID);
            sub?.Dispose();
        }
    }

    
    NotificationCompat.Builder? StartNotification(IHttpTransfer transfer)
    {
        this.EnsureChannel();
        NotificationCompat.Builder? builder = null;
        try
        {
            builder = new NotificationCompat.Builder(this.platform.AppContext, NotificationChannelId)
                .SetSmallIcon(this.platform.GetNotificationIconResource())
                .SetOngoing(true)
                .SetTicker("...")
                .SetContentTitle("Shiny HTTP Transfer Service")
                .SetContentText("Shiny service is running in the background")
                .SetProgress(100, 0, false);

            this.delegates
                .OfType<IAndroidForegroundServiceDelegate>()
                .ToList()
                .ForEach(x => x.Configure(builder));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting persistent notification for http transfers");
        }
        return builder;
    }


    public static string NotificationChannelId { get; set; } = "Transfers";
    protected virtual void EnsureChannel()
    {
        if (this.notifications!.GetNotificationChannel(NotificationChannelId) != null)
            return;

        var channel = new NotificationChannel(
            NotificationChannelId,
            NotificationChannelId,
            NotificationImportance.Default
        );
        channel.SetShowBadge(false);
        this.notifications.CreateNotificationChannel(channel);
    }
}