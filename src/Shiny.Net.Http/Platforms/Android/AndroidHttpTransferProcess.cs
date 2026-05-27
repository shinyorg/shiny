using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Net.Http;


public class AndroidHttpTransferProcess(
    ILogger<AndroidHttpTransferProcess> logger,
    IRepository repository,
    IConnectivity connectivity,
    IEnumerable<IHttpTransferDelegate> delegates
)
{
    readonly HttpClient httpClient = new();


    public static event EventHandler<HttpTransferResult>? ProgressOccurred;


    public void Run(Action onComplete)
    {
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Starting Transfer Loop Wait");
            using var cancelSrc = new CancellationTokenSource();

            EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> clearHandler = null!;
            clearHandler = (_, x) =>
            {
                if (x.EntityType == typeof(HttpTransfer) && x.Action == RepositoryAction.Clear)
                {
                    repository.ActionOccurred -= clearHandler;
                    logger.LogInformation("HTTP Transfers cleared - cancelling all transfers");
                    cancelSrc.Cancel();
                }
            };
            repository.ActionOccurred += clearHandler;
            using var sub = new RepoSub(() => repository.ActionOccurred -= clearHandler);

            try
            {
                var transfers = repository.GetAll<HttpTransfer>();
                while (!cancelSrc.IsCancellationRequested && transfers.Count > 0)
                {
                    logger.LogDebug("Starting Loop");
                    if (connectivity.IsInternetAvailable())
                    {
                        var full = connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Wifi);
                        logger.LogDebug("Internet Available - Trying Transfer Loop.  WIFI: " + full);

                        foreach (var transfer in transfers)
                        {
                            if (cancelSrc.IsCancellationRequested)
                            {
                                logger.LogDebug("Transfer Loop cancelled");
                            }
                            else if (!repository.Exists<HttpTransfer>(transfer.Identifier))
                            {
                                logger.LogDebug($"HTTP Transfer {transfer.Identifier} has been removed");
                            }
                            else if (transfer.Request.UseMeteredConnection || full)
                            {
                                logger.LogInformation($"Transfer {transfer.Identifier} starting");
                                await this.RunTransfer(transfer, cancelSrc.Token).ConfigureAwait(false);
                            }
                            else
                            {
                                logger.LogDebug($"Transfer {transfer.Identifier} is a metered transfer - waiting for WIFI");
                            }
                        }
                    }
                    else
                    {
                        logger.LogDebug("Internet Unavailable - Waiting for next pass");
                    }

                    transfers = repository.GetAll<HttpTransfer>();
                    if (transfers.Count > 0)
                    {
                        logger.LogDebug("Waiting for loop pass");
                        await Task
                            .Delay(10000, cancelSrc.Token)
                            .ConfigureAwait(false);
                    }
                }
                logger.LogDebug("All transfers complete");
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in transfer loop");
            }
            logger.LogDebug("Shutting down HTTP transfer service");
            onComplete();
        });
    }


    async Task RunTransfer(HttpTransfer transfer, CancellationToken cancelToken)
    {
        using var cancelSrc = new CancellationTokenSource();
        using var _ = cancelToken.Register(() => cancelSrc.Cancel());

        EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> removeHandler = null!;
        removeHandler = (_, x) =>
        {
            if (x.EntityType == typeof(HttpTransfer) &&
                x.Action == RepositoryAction.Remove &&
                transfer.Identifier.Equals(x.Entity!.Identifier))
            {
                repository.ActionOccurred -= removeHandler;
                logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc?.Cancel();
            }
        };
        repository.ActionOccurred += removeHandler;
        using var repoSub = new RepoSub(() => repository.ActionOccurred -= removeHandler);

        try
        {
            await this
                .DoRequest(transfer, cancelSrc.Token)
                .ConfigureAwait(false);

            logger.LogInformation("Completing Successful Transfer: " + transfer.Identifier);
            await delegates
                .RunDelegates(x => x.OnCompleted(transfer.Request), logger)
                .ConfigureAwait(false);

            repoSub.Dispose();
            repository.Remove(transfer);

            ProgressOccurred?.Invoke(null, new(
                transfer.Request,
                HttpTransferState.Completed,
                new TransferProgress(
                    0,
                    transfer.BytesToTransfer,
                    transfer.BytesTransferred
                ),
                null
            ));
        }
        catch (HttpRequestException ex)
        {
            repoSub.Dispose();
            repository.Remove(transfer);

            logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await delegates
                .RunDelegates(x => x.OnError(transfer!.Request, ex.StatusCode == null ? 0 : (int)ex.StatusCode, ex), logger)
                .ConfigureAwait(false);

            ProgressOccurred?.Invoke(null, new(
                transfer!.Request,
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
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
            this.PauseTransfer(transfer, "Error with transfer - " + ex, ex, isNetworkError: false);
        }
    }


    void PauseTransfer(HttpTransfer transfer, string reason, Exception exception, bool isNetworkError = true)
    {
        logger.StandardInfo(transfer.Identifier, reason + $" - {exception}");
        repository.Set(transfer with
        {
            Status = isNetworkError
                ? HttpTransferState.PausedByNoNetwork
                : HttpTransferState.Paused
        });
    }


    void PublishProgress(HttpTransfer transfer, TransferProgress x, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested)
            return;

        repository.Set(transfer with
        {
            Status = HttpTransferState.InProgress,
            BytesToTransfer = x.BytesToTransfer,
            BytesTransferred = x.BytesTransferred
        });

        ProgressOccurred?.Invoke(null, new HttpTransferResult(
            transfer.Request,
            HttpTransferState.InProgress,
            x,
            null
        ));
    }


    async Task DoRequest(HttpTransfer transfer, CancellationToken cancelToken)
    {
        var request = transfer.Request;
        var headers = request.Headers?.Select(x => (x.Key, x.Value)).ToArray();

        HttpMethod? httpMethod = null;
        if (request.HttpMethod != null)
            httpMethod = new HttpMethod(request.HttpMethod);

        HttpContent? bodyContent = null;
        var c = request.HttpContent;
        if (c != null)
            bodyContent = new StringContent(c.Content, Encoding.UTF8, c.ContentType);

        if (request.Type.IsUpload())
        {
            await this.httpClient.Upload(
                request.Uri,
                request.LocalFilePath,
                request.Type == TransferType.UploadMultipart,
                httpMethod,
                bodyContent,
                request.HttpContent?.ContentFormDataName ?? "value",
                request.FileFormDataName,
                headers,
                x => this.PublishProgress(transfer, x, cancelToken),
                cancelToken
            ).ConfigureAwait(false);
        }
        else
        {
            await this.httpClient.Download(
                request.Uri,
                request.LocalFilePath,
                8192,
                httpMethod,
                bodyContent,
                headers,
                x => this.PublishProgress(transfer, x, cancelToken),
                cancelToken
            ).ConfigureAwait(false);
        }
    }
}
