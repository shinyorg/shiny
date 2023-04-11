using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class TransferJob : IJob
{
    const int NOTIFICATION_ID = 222222;
    readonly NotificationManagerCompat notifications;
    readonly HttpClient httpClient = new();

    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IConnectivity connectivity;
    readonly IRepository repository;
    readonly IEnumerable<IHttpTransferDelegate> delegates;

    public TransferJob(
        ILogger<TransferJob> logger,
        AndroidPlatform platform,
        IRepository repository,
        IConnectivity connectivity,
        IEnumerable<IHttpTransferDelegate> delegates
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.repository = repository;
        this.connectivity = connectivity;        
        this.delegates = delegates;
        this.notifications = NotificationManagerCompat.From(platform.AppContext);
    }


    static readonly Subject<HttpTransferResult> progressSubj = new();
    public static IObservable<HttpTransferResult> WhenProgress() => progressSubj;


    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        var requests = this.repository.GetList<HttpTransfer>();
        var queue = new Queue<HttpTransfer>(requests);
        var cancelSrc = new CancellationTokenSource();

        this.repository
            .WhenActionOccurs()
            .Where(x =>
                x.EntityType == typeof(HttpTransfer) &&
                x.Action != RepositoryAction.Update
            )
            .Subscribe(x =>
            {
                switch (x.Action)
                {
                    case RepositoryAction.Add:
                        queue.Enqueue((HttpTransfer)x.Entity!);
                        break;

                    case RepositoryAction.Remove:
                        // TODO: if current request
                        cancelSrc?.Cancel();
                        break;

                    case RepositoryAction.Clear:
                        queue.Clear();
                        cancelSrc?.Cancel();
                        break;
                }
            });

        // TODO: if no-meter internet job is running, we also need to cancel
        // TODO: if internet is disconnected, job should be killed externally via the incoming cancel token
        // TODO: we don't error out of transfer, we simply pause it if the error is related to internet disconnect/timeout
        var request = queue.Dequeue();
        while (request != null)
        {
            //    if (transfer.Request.UseMeteredConnection || !this.connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet))
            cancelSrc = new();
            using var _ = cancelToken.Register(() => cancelSrc?.Cancel());

            await this.DoRequest(request, cancelSrc.Token).ConfigureAwait(false);
            request = queue.Dequeue();
        } 
    }


    async Task DoRequest(HttpTransfer transfer, CancellationToken cancelToken)
    {
        var request = transfer.Request;
        var headers = request.Headers?.Select(x => (x.Key, x.Value)).ToArray();

        //var method = new HttpMethod(request.HttpMethod ?? )
        var obs = request.IsUpload
            ? this.httpClient.Upload(request.Uri, request.LocalFilePath, null, headers)
            : this.httpClient.Download(request.Uri, request.LocalFilePath, 8192, null, headers);

        var builder = this.StartNotification(request);
        this.notifications.Notify(NOTIFICATION_ID, builder.Build());

        // TODO: trigger manager observable
        obs
            .Finally(() => this.notifications.Cancel(NOTIFICATION_ID))
            .Subscribe(
                x =>
                {
                    // TODO: need to be able to update HttpTransfer
                    builder.SetProgress(100, Convert.ToInt32(x.PercentComplete * 100), false);
                    this.notifications.Notify(NOTIFICATION_ID, builder.Build());

                    progressSubj.OnNext(new HttpTransferResult(
                        request,
                        HttpTransferState.InProgress,
                        x
                    ));
                },
                ex =>
                {
                    // TODO: need to be able to update HttpTransfer
                    this.delegates.RunDelegates(x => x.OnError(request, ex), this.logger);
                    progressSubj.OnNext(new HttpTransferResult(
                        request,
                        HttpTransferState.Error,
                        null
                    ));
                },
                () =>
                {
                    // TODO: need to be able to update HttpTransfer
                    // TODO: if not errored
                    this.delegates.RunDelegates(x => x.OnCompleted(request), this.logger);
                }
            );
    }

    
    NotificationCompat.Builder? StartNotification(HttpTransferRequest request)
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
                .OfType<IAndroidHttpTransferDelegate>()
                .ToList()
                .ForEach(x => x.ConfigureNotification(builder, request));
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