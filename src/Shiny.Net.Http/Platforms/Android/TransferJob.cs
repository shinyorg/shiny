using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;
using Shiny.Stores;
using Shiny.Stores.Impl;

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


    // TODO: what if we drop to an expensive network - we need to watch that as well
    // TODO: allow configurable amount of transfer at a time
    // TODO: check if progress is interdeterminate
    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        // this job is purely in android, so we have access to all android stuff        
        var expensiveNetwork = this.connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet);
        //this.connectivity
        //    .WhenInternetStatusChanged()
        //    .Subscribe(x =>
        //    {
        //        // TODO: a loss of internet will cause top level cancelToken to fire from Android
        //        // so only cancel if expensive network detected and current transfer ignore expensive network
        //    });

        // TODO: reloop for new transfers, check for cancelled transfers
        var currentTransfers = this.manager.Transfers.ToList();
        foreach (var transfer in currentTransfers)
        {
            if (transfer.Request.UseMeteredConnection || !expensiveNetwork)
            {                
                await this.manager.Resume(transfer.Identifier);                
                var builder = this.StartNotification(transfer);
                if (builder != null)
                    this.notifications.Notify(NOTIFICATION_ID, builder.Build());

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

                // cancel notification
                this.notifications.Cancel(NOTIFICATION_ID);
            }
        }
    }


    
    NotificationCompat.Builder? StartNotification(IHttpTransfer transfer)
    {
        NotificationCompat.Builder? builder = null;
        try
        {
            // TODO: android 13 POST_NOTIFICATION permissions
            builder = new NotificationCompat.Builder(this.platform.AppContext, "TODO: DEFAULT CHANNEL")
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


    //    static int idCount = 7999;

    //    public static string NotificationChannelId { get; set; } = "Service";
    //    protected virtual void EnsureChannel()
    //    {
    //        if (this.NotificationManager!.GetNotificationChannel(NotificationChannelId) != null)
    //            return;

    //        var channel = new NotificationChannel(
    //            NotificationChannelId,
    //            NotificationChannelId,
    //            NotificationImportance.Default
    //        );
    //        channel.SetShowBadge(false);
    //        this.NotificationManager.CreateNotificationChannel(channel);
    //    }
}