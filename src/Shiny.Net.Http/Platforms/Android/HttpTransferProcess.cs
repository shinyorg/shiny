using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
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

        progressSubj.Logger = logger;
    }


    static readonly ShinySubject<HttpTransferResult> progressSubj = new();
    public static IObservable<HttpTransferResult> WhenProgress() => progressSubj;
    

    public void Run(Action onComplete)
    {
        _ = Task.Run(async () =>
        {
            this.logger.LogInformation("Starting Transfer Loop Wait");
            var cancelSrc = new CancellationTokenSource();

            using var sub = this.repository
                .WhenActionOccurs()
                .Where(x =>
                    x.EntityType == typeof(HttpTransfer) &&
                    x.Action == RepositoryAction.Clear
                )
                .Take(1)
                .Subscribe(_ =>
                {
                    this.logger.LogInformation("HTTP Transfers cleared - cancelling all transfers");
                    cancelSrc.Cancel();
                });

            //var semaphore = new SemaphoreSlim(1, 2);
            try
            {
                var transfers = this.repository.GetList<HttpTransfer>();
                while (!cancelSrc.IsCancellationRequested && transfers.Count > 0)
                {
                    this.logger.LogDebug("Starting Loop");
                    if (this.connectivity.IsInternetAvailable())
                    {
                        var full = this.connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Wifi);
                        this.logger.LogDebug("Internet Available - Trying Transfer Loop.  WIFI: " + full);

                        foreach (var transfer in transfers)
                        {
                            var stillExists = this.repository.Exists<HttpTransfer>(transfer.Identifier);
                            if (cancelSrc.IsCancellationRequested)
                            {
                                this.logger.LogDebug("Transfer Loop cancelled");
                            }
                            else if (!this.repository.Exists<HttpTransfer>(transfer.Identifier))
                            {
                                this.logger.LogDebug($"HTTP Transfer {transfer.Identifier} has been removed");
                            }
                            else if (transfer.Request.UseMeteredConnection || full)
                            {
                                //this.logger.LogDebug("Checking Queue");
                                //await semaphore.WaitAsync(cancelSrc.Token);

                                //if (!cancelSrc.IsCancellationRequested)
                                //{
                                this.logger.LogInformation($"Transfer {transfer.Identifier} starting");
                                await this.RunTransfer(transfer, cancelSrc.Token).ConfigureAwait(false);

                                //this.RunTransfer(transfer, cancelSrc.Token)
                                //    .ContinueWith(_ =>
                                //    {
                                //        semaphore.Release();
                                //        this.logger.LogDebug("Releasing Semaphore");
                                //    });
                                //}
                            }
                            else
                            {
                                this.logger.LogDebug($"Transfer {transfer.Identifier} is a metered transfer - waiting for WIFI");
                            }
                        }
                    }
                    else
                    {
                        this.logger.LogDebug("Internet Unavailable - Waiting for next pass");
                    }

                    transfers = this.repository.GetList<HttpTransfer>();
                    if (transfers.Count > 0)
                    {
                        // TODO: configurable
                        this.logger.LogDebug("Waiting for loop pass");
                        await Task
                            .Delay(10000, cancelSrc.Token)
                            .ConfigureAwait(false);
                    }
                }
                this.logger.LogDebug("All transfers complete");
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in transfer loop");
            }
            this.logger.LogDebug("Shutting down HTTP transfer service");
            onComplete(); // shutdown service
        });
    }
    

    async Task RunTransfer(HttpTransfer transfer, CancellationToken cancelToken)
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
            .Take(1)
            .Subscribe(x =>
            {
                this.logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc?.Cancel();
            });

        try
        {
            await this
                .DoRequest(transfer, cancelSrc.Token)
                .ConfigureAwait(false);

            this.logger.LogInformation("Completing Successful Transfer: " + transfer.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnCompleted(transfer.Request), this.logger)
                .ConfigureAwait(false);

            progressSubj.OnNext(new(
                transfer.Request,
                HttpTransferState.Completed,
                new TransferProgress(
                    0,
                    transfer.BytesToTransfer,
                    transfer.BytesTransferred
                ),
                null
            ));
            repoSub.Dispose(); // dispose of this so cancellation isn't run

            this.repository.Remove(transfer);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnError(transfer!.Request, ex), this.logger)
                .ConfigureAwait(false);

            progressSubj.OnNext(new(
                transfer!.Request,
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
            repoSub.Dispose(); // dispose of this so cancellation isn't run

            this.repository.Remove(transfer);
        }
        catch (IOException ex) when (ex.InnerException is Java.Net.SocketException)
        {
            this.PauseTransfer(transfer, "Android Network Disconnected", ex);
        }
        catch (Java.Net.SocketException ex)
        {
            this.PauseTransfer(transfer, "Android Network Disconnected", ex);
        }
        catch (OperationCanceledException)
        {
            // transfer has been cancelled
        }
        catch (Exception ex)
        {
            // should always retry unless server fails
            this.PauseTransfer(transfer, "Error with transfer - " + ex.ToString(), ex);
        }
    }


    void PauseTransfer(HttpTransfer transfer, string reason, Exception exception)
    {
        this.logger.StandardInfo(transfer.Identifier, reason + $" - {exception}");
        this.repository.Set(transfer with
        {
            Status = HttpTransferState.PausedByNoNetwork
        });
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
                        BytesToTransfer = x.BytesToTransfer,
                        BytesTransferred = x.BytesTransferred
                    });

                    var result = new HttpTransferResult(
                        request,
                        HttpTransferState.InProgress,
                        x,
                        null
                    );
                    progressSubj.OnNext(result);
                }
            },
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult(null!)
        );

        await tcs.Task.ConfigureAwait(false);
    }
}