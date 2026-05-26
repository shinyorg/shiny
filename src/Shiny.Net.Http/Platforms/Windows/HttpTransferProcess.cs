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


    public static event EventHandler<HttpTransferResult>? ProgressOccurred;


    public void Run(Action onComplete)
    {
        _ = Task.Run(async () =>
        {
            this.logger.LogInformation("Starting Transfer Loop");
            using var cancelSrc = new CancellationTokenSource();

            EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> clearHandler = null!;
            clearHandler = (_, x) =>
            {
                if (x.EntityType == typeof(HttpTransfer) && x.Action == RepositoryAction.Clear)
                {
                    this.repository.ActionOccurred -= clearHandler;
                    this.logger.LogInformation("HTTP Transfers cleared - cancelling all transfers");
                    cancelSrc.Cancel();
                }
            };
            this.repository.ActionOccurred += clearHandler;
            using var sub = new RepoSub(() => this.repository.ActionOccurred -= clearHandler);

            try
            {
                var transfers = this.repository.GetAll<HttpTransfer>();
                while (!cancelSrc.IsCancellationRequested && transfers.Count > 0)
                {
                    this.logger.LogDebug("Starting Loop");
                    if (this.connectivity.IsInternetAvailable())
                    {
                        var full = this.connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Wifi);
                        this.logger.LogDebug("Internet Available - WIFI: " + full);

                        foreach (var transfer in transfers)
                        {
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
                                this.logger.LogInformation($"Transfer {transfer.Identifier} starting");
                                await this.RunTransfer(transfer, cancelSrc.Token).ConfigureAwait(false);
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

                    transfers = this.repository.GetAll<HttpTransfer>();
                    if (transfers.Count > 0)
                    {
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
            this.logger.LogDebug("Shutting down HTTP transfer process");
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
                this.repository.ActionOccurred -= removeHandler;
                this.logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc?.Cancel();
            }
        };
        this.repository.ActionOccurred += removeHandler;
        using var repoSub = new RepoSub(() => this.repository.ActionOccurred -= removeHandler);

        try
        {
            await this
                .DoRequest(transfer, cancelSrc.Token)
                .ConfigureAwait(false);

            this.logger.LogInformation("Completing Successful Transfer: " + transfer.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnCompleted(transfer.Request), this.logger)
                .ConfigureAwait(false);

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
            repoSub.Dispose();

            this.repository.Remove(transfer);
        }
        catch (HttpRequestException ex)
        {
            this.repository.Remove(transfer);

            this.logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnError(transfer!.Request, ex.StatusCode == null ? 0 : (int)ex.StatusCode, ex), this.logger)
                .ConfigureAwait(false);

            ProgressOccurred?.Invoke(null, new(
                transfer!.Request,
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
            repoSub.Dispose();
        }
        catch (IOException ex)
        {
            this.PauseTransfer(transfer, "Network Disconnected", ex);
        }
        catch (OperationCanceledException)
        {
            // transfer has been cancelled
        }
        catch (Exception ex)
        {
            this.PauseTransfer(transfer, "Error with transfer - " + ex, ex);
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


    void PublishProgress(HttpTransfer transfer, TransferProgress x, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested)
            return;

        this.repository.Set(transfer with
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
