using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Net.Http.Infrastructure;


public class HttpClientHttpTransferProcess(
    ILogger<HttpClientHttpTransferProcess> logger,
    IRepository repository,
    IConnectivity connectivity,
    IServiceProvider services,
    IHttpClientFactory httpClientFactory,
    TimeSpan? pollInterval = null
)
{
    /// <summary>
    /// Name of the <see cref="IHttpClientFactory"/> client used by the managed transfer loop.
    /// Registered via <c>services.AddHttpClient(...)</c>; apps can call
    /// <c>.ConfigureHttpClient(...)</c>/<c>.ConfigurePrimaryHttpMessageHandler(...)</c> against it.
    /// </summary>
    public const string HttpClientName = "Shiny.Net.Http";

    // HttpClient comes from IHttpClientFactory (handler pooling/lifetime, DI-configurable, and
    // fakeable in tests via a stub factory) - consistent with Shiny.Data.Sync's RestSyncTransport.
    // The inter-pass poll interval stays injectable so the loop can be unit-tested with a short tick.
    readonly HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
    readonly TimeSpan loopInterval = pollInterval ?? TimeSpan.FromSeconds(10);

    // Tracks the cancellation source of each in-flight transfer so a pause (repository status
    // flipped to Paused) can interrupt the active request without removing the transfer.
    readonly ConcurrentDictionary<string, CancellationTokenSource> activeTransfers = new();

    public static event EventHandler<HttpTransferResult>? ProgressOccurred;


    public void Run(Action onComplete)
    {
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Starting Transfer Loop");
            using var cancelSrc = new CancellationTokenSource();

            EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> repoHandler = null!;
            repoHandler = (_, x) =>
            {
                if (x.EntityType != typeof(HttpTransfer))
                    return;

                if (x.Action == RepositoryAction.Clear)
                {
                    repository.ActionOccurred -= repoHandler;
                    logger.LogInformation("HTTP Transfers cleared - cancelling all transfers");
                    cancelSrc.Cancel();
                }
                else if (x.Action == RepositoryAction.Update &&
                         x.Entity is HttpTransfer ht &&
                         ht.Status == HttpTransferState.Paused &&
                         this.activeTransfers.TryGetValue(ht.Identifier, out var transferCancel))
                {
                    logger.StandardInfo(ht.Identifier, "Pausing active transfer");
                    try
                    {
                        transferCancel.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // transfer already completed - nothing to pause
                    }
                }
            };
            repository.ActionOccurred += repoHandler;
            using var clearSub = new RepoSub(() => repository.ActionOccurred -= repoHandler);

            try
            {
                var transfers = repository.GetAll<HttpTransfer>();
                while (!cancelSrc.IsCancellationRequested && transfers.Count > 0)
                {
                    logger.LogDebug("Starting Loop");
                    if (connectivity.IsInternetAvailable())
                    {
                        var full = connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Wifi);
                        logger.LogDebug("Internet Available - WIFI: " + full);

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
                            else if (transfer.Status == HttpTransferState.Paused)
                            {
                                logger.LogDebug($"HTTP Transfer {transfer.Identifier} is paused - skipping");
                            }
                            else if (transfer.Request.UseMeteredConnection || full)
                            {
                                logger.LogInformation($"Transfer {transfer.Identifier} starting");
                                await this.RunTransfer(transfer, cancelSrc.Token).ConfigureAwait(false);
                            }
                            else
                            {
                                logger.LogDebug($"Transfer {transfer.Identifier} is a metered transfer - waiting for WIFI");
                                repository.Set(transfer with
                                {
                                    Status = HttpTransferState.PausedByCostedNetwork
                                });
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
                        await this
                            .WaitForNextPass(cancelSrc.Token)
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
            logger.LogDebug("Shutting down HTTP transfer process");
            onComplete();
        });
    }


    Task WaitForNextPass(CancellationToken cancelToken)
        => Task.Delay(this.loopInterval, cancelToken);


    async Task RunTransfer(HttpTransfer transfer, CancellationToken cancelToken)
    {
        using var cancelSrc = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        this.activeTransfers[transfer.Identifier] = cancelSrc;

        EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> removeHandler = null!;
        removeHandler = (_, x) =>
        {
            if (x.EntityType == typeof(HttpTransfer) &&
                x.Action == RepositoryAction.Remove &&
                transfer.Identifier.Equals(x.Entity!.Identifier))
            {
                repository.ActionOccurred -= removeHandler;
                logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc.Cancel();
            }
        };
        repository.ActionOccurred += removeHandler;
        using var repoSub = new RepoSub(() => repository.ActionOccurred -= removeHandler);
        using var activeSub = new RepoSub(() => this.activeTransfers.TryRemove(transfer.Identifier, out _));

        try
        {
            await this
                .DoRequest(transfer, cancelSrc.Token)
                .ConfigureAwait(false);

            logger.LogInformation("Completing Successful Transfer: " + transfer.Identifier);
            await services
                .RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(transfer.Request), logger)
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
            repository.Remove(transfer);
        }
        catch (HttpRequestException ex)
        {
            repository.Remove(transfer);

            logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await services
                .RunDelegates<IHttpTransferDelegate>(x => x.OnError(transfer!.Request, ex.StatusCode == null ? 0 : (int)ex.StatusCode, ex), logger)
                .ConfigureAwait(false);

            ProgressOccurred?.Invoke(null, new(
                transfer!.Request,
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
        }
        catch (IOException ex)
        {
            this.PauseTransfer(transfer, "Network Disconnected", ex);
        }
#if ANDROID
        catch (Java.Net.SocketException ex)
        {
            this.PauseTransfer(transfer, "Network Disconnected", ex);
        }
#endif
        catch (OperationCanceledException)
        {
            // transfer has been cancelled or removed - nothing to do
        }
        catch (Exception ex)
        {
            this.PauseTransfer(transfer, "Error with transfer - " + ex, ex);
        }
    }


    void PauseTransfer(HttpTransfer transfer, string reason, Exception exception)
    {
        logger.StandardInfo(transfer.Identifier, reason + $" - {exception}");
        if (repository.Exists<HttpTransfer>(transfer.Identifier))
        {
            repository.Set(transfer with
            {
                Status = HttpTransferState.PausedByNoNetwork
            });
        }
    }


    async Task DoRequest(HttpTransfer transfer, CancellationToken cancelToken)
    {
        if (transfer.Request.Type == TransferType.Download)
        {
            await this.DoDownload(transfer, cancelToken).ConfigureAwait(false);
        }
        else
        {
            await this.DoUpload(transfer, cancelToken).ConfigureAwait(false);
        }
    }


    async Task DoUpload(HttpTransfer transfer, CancellationToken cancelToken)
    {
        // uploads are NOT resumable - if a previous attempt left partial state, restart from zero
        var request = transfer.Request;
        var headers = request.Headers?.Select(x => (x.Key, x.Value)).ToArray();

        HttpMethod? httpMethod = null;
        if (request.HttpMethod != null)
            httpMethod = new HttpMethod(request.HttpMethod);

        HttpContent? bodyContent = null;
        var c = transfer.Request.HttpContent;
        if (c != null)
            bodyContent = new StringContent(c.Content, Encoding.UTF8, c.ContentType);

        await this.httpClient.Upload(
            request.Uri,
            request.LocalFilePath,
            request.Type == TransferType.UploadMultipart,
            httpMethod,
            bodyContent,
            request.HttpContent?.ContentFormDataName ?? "value",
            request.FileFormDataName,
            headers,
            x => this.PublishProgress(transfer, x),
            cancelToken
        ).ConfigureAwait(false);
    }


    async Task DoDownload(HttpTransfer transfer, CancellationToken cancelToken)
    {
        var request = transfer.Request;

        // resumable: figure out where the existing partial file leaves off (if any)
        long startOffset = 0;
        if (File.Exists(request.LocalFilePath))
            startOffset = new FileInfo(request.LocalFilePath).Length;

        var httpReq = new HttpRequestMessage();
        httpReq.Method = request.HttpMethod != null ? new HttpMethod(request.HttpMethod) : HttpMethod.Get;
        httpReq.RequestUri = new Uri(request.Uri);

        var c = request.HttpContent;
        if (c != null)
            httpReq.Content = new StringContent(c.Content, Encoding.UTF8, c.ContentType);

        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
                httpReq.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (startOffset > 0)
        {
            httpReq.Headers.Range = new RangeHeaderValue(startOffset, null);
            logger.StandardInfo(request.Identifier, $"Resuming download from byte {startOffset}");
        }

        using var response = await this.httpClient
            .SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cancelToken)
            .ConfigureAwait(false);

        // server didn't honor range -> start over from byte 0
        var append = response.StatusCode == HttpStatusCode.PartialContent && startOffset > 0;
        if (!append && startOffset > 0)
        {
            logger.StandardInfo(request.Identifier, "Server ignored Range header - restarting download");
            startOffset = 0;
        }

        response.EnsureSuccessStatusCode();

        long? totalBytes = null;
        if (response.Content.Headers.ContentLength.HasValue)
        {
            totalBytes = append
                ? startOffset + response.Content.Headers.ContentLength.Value
                : response.Content.Headers.ContentLength.Value;
        }
        else if (response.Content.Headers.ContentRange?.Length != null)
        {
            totalBytes = response.Content.Headers.ContentRange.Length;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(request.LocalFilePath)!);

        using var source = await response.Content.ReadAsStreamAsync(cancelToken).ConfigureAwait(false);
        using var dest = new FileStream(
            request.LocalFilePath,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.Read
        );

        var buffer = new byte[8192];
        var totalBytesXfer = startOffset;
        var totalSince = 0L;
        var stop = Stopwatch.StartNew();

        // initial progress update so the persisted state reflects the resume offset
        this.PublishProgress(transfer, new TransferProgress(0, totalBytes, totalBytesXfer));

        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false)) != 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, bytesRead), cancelToken).ConfigureAwait(false);
            totalSince += bytesRead;
            totalBytesXfer += bytesRead;

            if (totalBytes.HasValue && totalBytesXfer == totalBytes.Value)
            {
                stop.Stop();
                this.PublishProgress(transfer, new TransferProgress(0, totalBytes, totalBytesXfer));
            }
            else if (stop.Elapsed.TotalSeconds > 2)
            {
                var bps = Convert.ToInt64(totalSince / stop.Elapsed.TotalSeconds);
                this.PublishProgress(transfer, new TransferProgress(bps, totalBytes, totalBytesXfer));
                totalSince = 0;
                stop.Restart();
            }
        }
    }


    void PublishProgress(HttpTransfer transfer, TransferProgress progress)
    {
        // A pause flips the persisted status to Paused while the request is still unwinding.
        // Don't resurrect a paused (or removed) transfer with a late progress tick.
        var current = repository.Get<HttpTransfer>(transfer.Identifier);
        if (current == null || current.Status == HttpTransferState.Paused)
            return;

        repository.Set(transfer with
        {
            Status = HttpTransferState.InProgress,
            BytesToTransfer = progress.BytesToTransfer,
            BytesTransferred = progress.BytesTransferred
        });

        ProgressOccurred?.Invoke(null, new HttpTransferResult(
            transfer.Request,
            HttpTransferState.InProgress,
            progress,
            null
        ));
    }


}
