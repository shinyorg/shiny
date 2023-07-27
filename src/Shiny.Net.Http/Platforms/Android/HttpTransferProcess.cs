using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferProcess
{
    const int NOTIFICATION_ID = 222222;
    readonly NotificationManagerCompat notifications;
    readonly HttpClient httpClient = new();

    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IConnectivity connectivity;
    readonly IRepository repository;
    readonly IEnumerable<IHttpTransferDelegate> delegates;

    public HttpTransferProcess(
        ILogger<HttpTransferProcess> logger,
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


    async Task Run(CancellationToken cancelToken)
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
                        this.logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                        cancelSrc?.Cancel();
                    }
                }
                // TODO: fire subject for cancel
            })
            .DisposedBy(disposer);

        this.connectivity
            .WhenChanged()
            .Where(_ => activeTransfer != null)
            .Subscribe(x =>
            {
                if (!x.IsInternetAvailable())
                {
                    this.logger.StandardInfo(activeTransfer!.Identifier, "No network detected for transfers - pausing");
                    this.repository.Set(activeTransfer! with
                    {
                        Status = HttpTransferState.PausedByNoNetwork
                    });
                    // don't cancel here, the job or the transfer itself will do it
                }
                else if (x.Access == NetworkAccess.ConstrainedInternet && !activeTransfer!.Request.UseMeteredConnection)
                {
                    this.logger.StandardInfo(activeTransfer!.Identifier, "Costed network detected for active transfer - pausing");
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
                    this.logger.StandardInfo(activeTransfer!.Identifier, "Starting Transfer");
                    cancelSrc = new();
                    using var _ = cancelToken.Register(() => cancelSrc?.Cancel());

                    await this.DoRequest(activeTransfer, cancelSrc.Token).ConfigureAwait(false);
                    this.logger.StandardInfo(activeTransfer!.Identifier, "Finished Transfer");

                    this.repository.Remove<HttpTransfer>(activeTransfer.Identifier);
                }
                else
                {
                    this.logger.StandardInfo(activeTransfer!.Identifier, "Cannot start on current network configuration. Waiting for next pass");
                }
            }
            catch (TaskCanceledException)
            {
                this.logger.StandardInfo(activeTransfer!.Identifier, "Suspend Requested");
            }
            catch (Exception ex)
            {
                this.repository.Remove<HttpTransfer>(activeTransfer.Identifier);
                this.logger.LogError(ex, "There was an error processing transfer: " + activeTransfer?.Identifier);
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

        HttpContent? bodyContent = null;
        var c = transfer.Request.HttpContent;
        if (c != null)
        {
            bodyContent = new StringContent(c.Content, Encoding.UTF8, c.ContentType);
        }
        var obs = request.IsUpload
            ? this.httpClient.Upload(
                request.Uri,
                request.LocalFilePath,
                httpMethod,
                bodyContent,
                request.HttpContent?.ContentFormDataName ?? "value",
                request.FileFormDataName,
                headers
            )
            : this.httpClient.Download(
                request.Uri,
                request.LocalFilePath,
                8192,
                httpMethod,
                bodyContent,
                headers
            );

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
                        x,
                        null
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
                        TransferProgress.Empty,
                        ex
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
                    ),
                    null
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