using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
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
        if (requests.Count == 0)
            return;

        var cancelSrc = new CancellationTokenSource();
        var disposer = new CompositeDisposable();
        HttpTransfer? activeTransfer = null;

        this.repository
            .WhenActionOccurs()
            .Where(x =>
                x.EntityType == typeof(HttpTransfer) &&
                (
                    x.Action == RepositoryAction.Remove ||
                    x.Action == RepositoryAction.Clear
                )
            )
            .Subscribe(x =>
            {
                if (x.Action == RepositoryAction.Clear)
                {
                    this.logger.LogDebug("All HTTP Transfers have been cleared");
                    cancelSrc?.Cancel();
                }
                else
                {
                    var transfer = (HttpTransfer)x.Entity!;
                    if (transfer.Identifier == activeTransfer?.Identifier)
                    {
                        this.logger.LogDebug("Current transfer has been removed");
                        cancelSrc?.Cancel();
                    }
                }
            })
            .DisposedBy(disposer);

        this.connectivity
            .WhenChanged()
            .Where(_ => activeTransfer != null)
            .Subscribe(x =>
            {
                if (!x.IsInternetAvailable())
                {
                    this.logger.LogDebug("No network detected for transfers - pausing");
                    this.repository.Set(activeTransfer! with
                    {
                        Status = HttpTransferState.PausedByNoNetwork
                    });
                    // don't cancel here, the job or the transfer itself will do it
                }
                else if (x.Access == NetworkAccess.ConstrainedInternet && !activeTransfer!.Request.UseMeteredConnection)
                {
                    this.logger.LogDebug("Costed network detected for active transfer - pausing");
                    this.repository.Set(activeTransfer! with
                    {
                        Status = HttpTransferState.PausedByCostedNetwork
                    });
                    cancelSrc?.Cancel();
                }
            })
            .DisposedBy(disposer);

        activeTransfer = this.repository
            .GetList<HttpTransfer>()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefault();

        while (activeTransfer != null && !cancelToken.IsCancellationRequested)
        {
            try
            {
                if (activeTransfer.Request.UseMeteredConnection || !this.connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet))
                {
                    this.logger.LogDebug("Staring transfer: " + activeTransfer.Identifier);
                    cancelSrc = new();
                    using var _ = cancelToken.Register(() => cancelSrc?.Cancel());

                    await this.DoRequest(activeTransfer, cancelSrc.Token).ConfigureAwait(false);
                    this.logger.LogDebug("Finished transfer: " + activeTransfer.Identifier);

                    this.repository.Remove<HttpTransfer>(activeTransfer.Identifier);
                }
                else
                {
                    this.logger.LogDebug($"Transfer '{activeTransfer.Identifier}' cannot start on current network configuration. Waiting for next pass");
                }
            }
            catch (TaskCanceledException)
            {
                this.logger.LogDebug("Job is being told to suspend");
            }
            catch (Exception ex)
            {
                this.repository.Remove<HttpTransfer>(activeTransfer.Identifier);
                this.logger.LogError(ex, "There was an isssue processing request");
            }
            activeTransfer = this.repository
                .GetList<HttpTransfer>()
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefault();
        }
        disposer.Dispose();
    }


    async Task DoRequest(HttpTransfer transfer, CancellationToken cancelToken)
    {
        var request = transfer.Request;
        var headers = request.Headers?.Select(x => (x.Key, x.Value)).ToArray() ?? Array.Empty<(string Key, string Value)>();

        HttpMethod? httpMethod = null;
        if (request.HttpMethod != null)
            httpMethod = new HttpMethod(request.HttpMethod);

        var obs = request.IsUpload
            ? this.httpClient.Upload(request.Uri, request.LocalFilePath, httpMethod, headers)
            : this.httpClient.Download(request.Uri, request.LocalFilePath, 8192, httpMethod, headers);

        var builder = this.StartNotification(request);
        this.notifications.Notify(NOTIFICATION_ID, builder.Build());

        var tcs = new TaskCompletionSource<object>();
        using var _ = cancelToken.Register(() => tcs.TrySetCanceled());

        var sub = obs.Subscribe(
            x =>
            {
                if (!cancelToken.IsCancellationRequested)
                {
                    this.repository.Set(transfer with
                    {
                        Status = HttpTransferState.InProgress,
                        BytesToTransfer = x.IsDeterministic ? x.BytesToTransfer : null,
                        BytesTransferred = x.BytesTransferred
                    });

                    // allow customization here though android delegate?
                    builder.SetProgress(100, Convert.ToInt32(x.PercentComplete * 100), false);
                    this.notifications.Notify(NOTIFICATION_ID, builder.Build());

                    progressSubj.OnNext(new HttpTransferResult(
                        request,
                        HttpTransferState.InProgress,
                        x
                    ));
                }
            },
            ex =>
            {
                if (ex is not TaskCanceledException)
                {
                    this.delegates.RunDelegates(x => x.OnError(request, ex), this.logger);

                    progressSubj.OnNext(new HttpTransferResult(
                        request,
                        HttpTransferState.Error,
                        new TransferProgress(0, 0, 0)
                    ));
                }
                tcs.TrySetResult(ex);
            },
            () =>
            {
                this.delegates.RunDelegates(x => x.OnCompleted(request), this.logger);
                progressSubj.OnNext(new HttpTransferResult(
                    request,
                    HttpTransferState.Completed,
                    new TransferProgress(
                        0,
                        transfer.BytesToTransfer,
                        transfer.BytesTransferred
                    )
                ));
                tcs.TrySetResult(null!);
            }
        );

        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            this.notifications.Cancel(NOTIFICATION_ID);
            sub.Dispose();
        }
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
            NotificationImportance.Low // to disable sound
        );
        channel.SetShowBadge(false);
        channel.SetSound(null, null);
        this.notifications.CreateNotificationChannel(channel);
    }
}