using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferProcess
{
    readonly HttpClient httpClient = new();

    readonly ILogger logger;
    readonly IConnectivity connectivity;
    readonly IRepository repository;
    readonly IEnumerable<IHttpTransferDelegate> delegates;


    public HttpTransferProcess(
        ILogger<HttpTransferProcess> logger,
        IRepository repository,
        IConnectivity connectivity,
        IEnumerable<IHttpTransferDelegate> delegates
    )
    {
        this.logger = logger;
        this.repository = repository;
        this.connectivity = connectivity;        
        this.delegates = delegates;
    }


    static readonly Subject<HttpTransferResult> progressSubj = new();
    public static IObservable<HttpTransferResult> WhenProgress() => progressSubj;
    CompositeDisposable disposer = null!;
    

    public void Run(HttpTransferProcessArgs args)
    {
        this.disposer = new();
        CancellationTokenSource cancelSrc = null!;
        ConcurrentQueue<HttpTransfer> queue = new();

        this.connectivity
            .WhenInternetStatusChanged()
            .Where(x => x == true)
            .DistinctUntilChanged()
            .Subscribe(_ =>
            {
                try
                {
                    var fullInternet = this.connectivity.Access == NetworkAccess.Internet;

                    var queue = new ConcurrentQueue<HttpTransfer>(this.repository
                        .GetList<HttpTransfer>(x =>
                            !x.Request.UseMeteredConnection ||
                            fullInternet
                        )
                        .OrderBy(x => x.CreatedAt)
                        .ToList()
                    );
                    // this will start with an event triggering everything
                    // current transfers will cancel themselves and set their state when connectivity drops
                    this.logger.LogDebug("Connectivity is restored - starting transfers");
                    cancelSrc = new();

                    this.TransferLoop(args, queue, cancelSrc.Token).ContinueWith(x =>
                    {
                        if (x.Exception != null)
                            this.logger.LogError(x.Exception, "Transfer loop failed");
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error in connectivity event");
                }
            })
            .DisposedBy(this.disposer);

        this.repository
            .WhenActionOccurs()
            .Where(x => x.EntityType == typeof(HttpTransfer))
            .Subscribe(x =>
            {
                try
                {
                    switch (x.Action)
                    {
                        case RepositoryAction.Add:
                            queue.Enqueue((HttpTransfer)x.Entity!);
                            break;

                        case RepositoryAction.Remove:
                            var transfers = this.repository.GetList<HttpTransfer>();
                            if (transfers.Count == 0)
                            {
                                this.logger.LogInformation("All transfers completed - shutting down");
                                this.disposer.Dispose();
                                args.OnComplete();
                            }
                            else
                            {
                                queue = new ConcurrentQueue<HttpTransfer>(transfers);
                                this.logger.LogInformation($"{transfers.Count} transfers remaining after remove");
                            }
                            break;

                        case RepositoryAction.Clear:
                            this.logger.LogDebug("All HTTP Transfers have been cleared");
                            cancelSrc?.Cancel();
                            this.disposer.Dispose();
                            args.OnComplete();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error in repository action");
                }
            })
            .DisposedBy(this.disposer);
    }



    async Task TransferLoop(HttpTransferProcessArgs args, ConcurrentQueue<HttpTransfer> transfers, CancellationToken cancelToken)
    {
        while (transfers.TryDequeue(out var transfer) &&
               this.connectivity.IsInternetAvailable() &&
               !cancelToken.IsCancellationRequested
        )
        {
            this.logger.LogDebug("LOOP: Starting Transfer - " + transfer.Identifier);
            this.UpdateTransferNotification(args, transfer);
            await this.RunTransfer(args, transfer, cancelToken).ConfigureAwait(false);

            this.logger.LogDebug("LOOP: Stopping/Finished Transfer - " + transfer.Identifier);
        }
    }


    async Task RunTransfer(HttpTransferProcessArgs args, HttpTransfer transfer, CancellationToken cancelToken)
    {
        var cancelSrc = new CancellationTokenSource();
        using var _ = cancelToken.Register(() => cancelSrc.Cancel());

        using var repoSub = this.repository
            .WhenActionOccurs()
            .Where(x =>
                x.EntityType == typeof(HttpTransfer) &&
                x.Action == RepositoryAction.Remove &&
                transfer.Identifier.Equals(x.Entity!.Identifier)
            )
            .Subscribe(x =>
            {
                this.logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc?.Cancel();
            });

        using var connSub = this.connectivity
            .WhenInternetStatusChanged()
            .Where(x => !x)
            .Subscribe(_ =>
            {
                this.logger.StandardInfo(transfer.Identifier, "No network detected for transfers - pausing");
                this.repository.Set(transfer with
                {
                    Status = HttpTransferState.PausedByNoNetwork
                });
            });

        try
        {
            await this
                .DoRequest(args, transfer, cancelSrc.Token)
                .ConfigureAwait(false);

            this.logger.LogInformation("Completing Successful Transfer: " + transfer.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnCompleted(transfer.Request), this.logger)
                .ConfigureAwait(false);

            progressSubj.OnNext(new HttpTransferResult(
                transfer.Request,
                HttpTransferState.Completed,
                new TransferProgress(
                    0,
                    transfer.BytesToTransfer,
                    transfer.BytesTransferred
                ),
                null
            ));
            this.repository.Remove(transfer);
        }
        catch (HttpRequestException ex)
        {
            this.repository.Remove(transfer);
            this.logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnError(transfer!.Request, ex), this.logger)
                .ConfigureAwait(false);

            progressSubj.OnNext(new HttpTransferResult(
                transfer!.Request,
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
        }
        catch (OperationCanceledException)
        {
            this.logger.StandardInfo(transfer!.Identifier, "Suspend Requested");
        }
        // should always retry unless server fails
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Error with transfer");
        }
    }


    void UpdateTransferNotification(HttpTransferProcessArgs args, HttpTransfer transfer)
    {
        this.logger.LogDebug("Updating Foreground Notification");
        var percentComplete = transfer.IsDeterministic ? Convert.ToInt32(transfer.PercentComplete()! * 100) : 0;

        args.Builder.SetContentText("Processing Background Transfers");
        args.Builder.SetProgress(
            100,
            percentComplete,
            !transfer.IsDeterministic
        );
        this.delegates
            .OfType<IAndroidHttpTransferDelegate>()
            .ToList()
            .ForEach(x =>
            {
                try
                {
                    x.ConfigureNotification(args.Builder, transfer);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, $"Error updating notification on user delegate: {x.GetType().FullName}");
                }
            });

        args.SendNotification();
        this.logger.LogDebug("Updated Foreground Notification");
    }


    async Task DoRequest(HttpTransferProcessArgs args, HttpTransfer transfer, CancellationToken cancelToken)
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

        var tcs = new TaskCompletionSource<object>();
        using var _ = cancelToken.Register(() => tcs.TrySetCanceled());

        using var sub = obs.Subscribe(
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

                    if (x.IsDeterministic)
                        this.UpdateTransferNotification(args, transfer);

                    progressSubj.OnNext(new HttpTransferResult(
                        request,
                        HttpTransferState.InProgress,
                        x,
                        null
                    ));
                }
            },
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult(null!)
        );

        await tcs.Task.ConfigureAwait(false);
    }
}