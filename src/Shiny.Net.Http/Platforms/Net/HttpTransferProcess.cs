using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            this.logger.LogInformation("Starting Transfer Loop");
            var cancelSrc = new CancellationTokenSource();

            using var clearSub = this.repository
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

            // bump the loop whenever connectivity changes so paused transfers wake up immediately
            using var connSub = this.connectivity
                .WhenChanged()
                .Subscribe(_ => this.connectivityChanged.Set());

            try
            {
                var transfers = this.repository.GetList<HttpTransfer>();
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
                                this.repository.Set(transfer with
                                {
                                    Status = HttpTransferState.PausedByCostedNetwork
                                });
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
                        this.logger.LogDebug("Waiting for loop pass");
                        await this
                            .WaitForNextPass(cancelSrc.Token)
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


    readonly AsyncManualResetEvent connectivityChanged = new();
    async Task WaitForNextPass(CancellationToken cancelToken)
    {
        // wake on connectivity change OR after a 10s polling interval
        var delayTask = Task.Delay(10000, cancelToken);
        var connTask = this.connectivityChanged.WaitAsync(cancelToken);
        await Task.WhenAny(delayTask, connTask).ConfigureAwait(false);
        this.connectivityChanged.Reset();
    }


    async Task RunTransfer(HttpTransfer transfer, CancellationToken cancelToken)
    {
        var cancelSrc = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

        using var repoSub = this.repository
            .WhenActionOccurs()
            .Where(x =>
                x.EntityType == typeof(HttpTransfer) &&
                x.Action == RepositoryAction.Remove &&
                transfer.Identifier.Equals(x.Entity!.Identifier)
            )
            .Take(1)
            .Subscribe(_ =>
            {
                this.logger.StandardInfo(transfer.Identifier, "Current transfer has been removed");
                cancelSrc.Cancel();
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
            this.repository.Remove(transfer);
        }
        catch (HttpRequestException ex)
        {
            this.repository.Remove(transfer);

            this.logger.LogError(ex, "There was an error processing transfer: " + transfer?.Identifier);
            await this.delegates
                .RunDelegates(x => x.OnError(transfer!.Request, ex.StatusCode == null ? 0 : (int)ex.StatusCode, ex), this.logger)
                .ConfigureAwait(false);

            progressSubj.OnNext(new(
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
        this.logger.StandardInfo(transfer.Identifier, reason + $" - {exception}");
        if (this.repository.Exists<HttpTransfer>(transfer.Identifier))
        {
            this.repository.Set(transfer with
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
        var headers = request.Headers?.Select(x => (x.Key, x.Value)).ToArray() ?? Array.Empty<(string Key, string Value)>();

        HttpMethod? httpMethod = null;
        if (request.HttpMethod != null)
            httpMethod = new HttpMethod(request.HttpMethod);

        HttpContent? bodyContent = null;
        var c = transfer.Request.HttpContent;
        if (c != null)
            bodyContent = new StringContent(c.Content, Encoding.UTF8, c.ContentType);

        var obs = this.httpClient.Upload(
            request.Uri,
            request.LocalFilePath,
            request.Type == TransferType.UploadMultipart,
            httpMethod,
            bodyContent,
            request.HttpContent?.ContentFormDataName ?? "value",
            request.FileFormDataName,
            headers
        );

        await this.PumpProgress(transfer, obs, cancelToken).ConfigureAwait(false);
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
            this.logger.StandardInfo(request.Identifier, $"Resuming download from byte {startOffset}");
        }

        using var response = await this.httpClient
            .SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cancelToken)
            .ConfigureAwait(false);

        // server didn't honor range -> start over from byte 0
        var append = response.StatusCode == HttpStatusCode.PartialContent && startOffset > 0;
        if (!append && startOffset > 0)
        {
            this.logger.StandardInfo(request.Identifier, "Server ignored Range header - restarting download");
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


    async Task PumpProgress(HttpTransfer transfer, IObservable<TransferProgress> obs, CancellationToken cancelToken)
    {
        var tcs = new TaskCompletionSource<object>();
        using var _ = cancelToken.Register(() => tcs.TrySetCanceled());

        using var sub = obs.Subscribe(
            x =>
            {
                if (!cancelToken.IsCancellationRequested)
                    this.PublishProgress(transfer, x);
            },
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult(null!)
        );

        await tcs.Task.ConfigureAwait(false);
    }


    void PublishProgress(HttpTransfer transfer, TransferProgress progress)
    {
        if (this.repository.Exists<HttpTransfer>(transfer.Identifier))
        {
            this.repository.Set(transfer with
            {
                Status = HttpTransferState.InProgress,
                BytesToTransfer = progress.BytesToTransfer,
                BytesTransferred = progress.BytesTransferred
            });
        }

        progressSubj.OnNext(new HttpTransferResult(
            transfer.Request,
            HttpTransferState.InProgress,
            progress,
            null
        ));
    }


    sealed class AsyncManualResetEvent
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitAsync(CancellationToken ct)
        {
            var t = this.tcs.Task;
            if (!ct.CanBeCanceled)
                return t;

            return Task.WhenAny(t, Task.Delay(Timeout.Infinite, ct));
        }

        public void Set() => this.tcs.TrySetResult(true);

        public void Reset()
        {
            while (true)
            {
                var current = this.tcs;
                if (!current.Task.IsCompleted)
                    return;
                var fresh = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (Interlocked.CompareExchange(ref this.tcs, fresh, current) == current)
                    return;
            }
        }
    }
}
