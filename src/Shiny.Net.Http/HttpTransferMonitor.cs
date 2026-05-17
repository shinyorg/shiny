using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Collections;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


/// <summary>
/// Helper for binding the current set of HTTP transfers to a user interface,
/// surfacing add/remove/progress changes via an observable collection.
/// </summary>
public class HttpTransferMonitor(
    IHttpTransferManager manager,
    IRepository repository,
    ILogger<HttpTransferMonitor> logger
) : IDisposable
{
    readonly SemaphoreSlim startLock = new(1, 1);
    IDisposable? repoSub;
    IDisposable? updateSub;


    readonly BindingList<HttpTransferObject> transfers = new();

    /// <summary>
    /// Observable collection of transfers, suitable for binding to UI controls.
    /// </summary>
    public INotifyReadOnlyCollection<HttpTransferObject> Transfers => this.transfers;

    /// <summary>
    /// Gets a value indicating whether the monitor is currently subscribed for updates.
    /// </summary>
    public bool IsStarted => this.repoSub != null || this.updateSub != null;


    /// <summary>
    /// Removes entries in terminal states from the bound collection.
    /// </summary>
    /// <param name="removeFinished">Remove completed transfers.</param>
    /// <param name="removeCancelled">Remove cancelled transfers.</param>
    /// <param name="removeErrors">Remove transfers in an error state.</param>
    public void Clear(bool removeFinished, bool removeCancelled, bool removeErrors)
    {
        var list = this.transfers.ToList();
        var toRemove = new List<HttpTransferObject>();

        foreach (var item in list)
        {
            switch (item.Status)
            {
                case HttpTransferState.Completed:
                    if (removeFinished)
                        toRemove.Add(item);
                    break;

                case HttpTransferState.Canceled:
                    if (removeCancelled)
                        toRemove.Add(item);
                    break;

                case HttpTransferState.Error:
                    if (removeErrors)
                        toRemove.Add(item);
                    break;
            }
        }
        if (toRemove.Count > 0)
            this.transfers.RemoveRange(toRemove);
    }


    /// <summary>
    /// Starts monitoring and seeds the collection with currently pending transfers.
    /// </summary>
    /// <param name="removeFinished">Auto-remove transfers from the collection on completion.</param>
    /// <param name="removeErrors">Auto-remove transfers from the collection when they error.</param>
    /// <param name="removeCancelled">Auto-remove transfers from the collection when cancelled.</param>
    /// <param name="syncContext">Optional UI synchronization context for marshalling updates.</param>
    public async Task Start(
        bool removeFinished = true,
        bool removeErrors = true,
        bool removeCancelled = true,
        SynchronizationContext? syncContext = null
    )
    {
        await this.startLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (this.repoSub != null)
                throw new InvalidOperationException("Already running monitor");
        }
        finally
        {
            this.startLock.Release();
        }

        this.transfers.Clear();

        var current = (await manager.GetTransfers())
            .Select(x =>
            {
                var obj = new HttpTransferObject(x.Request);
                obj.Update(new HttpTransferResult(
                    x.Request,
                    x.Status,
                    new TransferProgress(
                        0,
                        x.BytesToTransfer,
                        x.BytesTransferred
                    ),
                    null
                ));
                return obj;
            });

        this.transfers.AddRange(current);

        EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> repoHandler = (_, x) =>
        {
            if (x.EntityType != typeof(HttpTransfer))
                return;

            void process()
            {
                if (x.Action == RepositoryAction.Clear)
                {
                    logger.LogInformation("Incoming HTTP Transfer Repository Clear");
                    this.transfers.Clear();
                }
                else
                {
                    var e = (HttpTransfer)x.Entity!;
                    logger.RepositoryChange(x.Action, e.Identifier, e.Request.Uri);

                    switch (x.Action)
                    {
                        case RepositoryAction.Add:
                            this.transfers.Add(new HttpTransferObject(e.Request));
                            break;

                        case RepositoryAction.Remove:
                            var vm = this.transfers.FirstOrDefault(y => y.Identifier.Equals(e.Identifier));
                            if (vm != null)
                                this.transfers.Remove(vm);
                            break;
                    }
                }
            }

            if (syncContext != null)
                syncContext.Post(_ => process(), null);
            else
                process();
        };
        repository.ActionOccurred += repoHandler;
        this.repoSub = new RepoSub(() => repository.ActionOccurred -= repoHandler);

        EventHandler<HttpTransferResult> updateHandler = (_, x) =>
        {
            void process()
            {
                var item = this.transfers.FirstOrDefault(y => y.Identifier.Equals(x.Request.Identifier));
                if (item == null)
                {
                    item = new HttpTransferObject(x.Request);
                    this.transfers.Add(item);
                }

                item.Update(x);
                logger.TransferUpdate(item.Identifier, item.Status);

                switch (x.Status)
                {
                    case HttpTransferState.Completed:
                        if (removeFinished)
                            this.transfers.Remove(item);
                        break;

                    case HttpTransferState.Canceled:
                        if (removeCancelled)
                            this.transfers.Remove(item);
                        break;

                    case HttpTransferState.Error:
                        if (removeErrors)
                            this.transfers.Remove(item);
                        break;
                }
            }

            if (syncContext != null)
                syncContext.Post(_ => process(), null);
            else
                process();
        };
        manager.UpdateReceived += updateHandler;
        this.updateSub = new RepoSub(() => manager.UpdateReceived -= updateHandler);
    }


    /// <summary>
    /// Stops monitoring and detaches event handlers.
    /// </summary>
    public void Stop()
    {
        this.repoSub?.Dispose();
        this.updateSub?.Dispose();
        this.repoSub = null;
        this.updateSub = null;
    }

    /// <inheritdoc />
    public void Dispose() => this.Stop();
}


/// <summary>
/// Observable view-model representing a single HTTP transfer for UI binding.
/// </summary>
public class HttpTransferObject : NotifyPropertyChanged
{
    /// <summary>
    /// Creates a new view-model for the given request.
    /// </summary>
    /// <param name="request">The transfer request being represented.</param>
    public HttpTransferObject(HttpTransferRequest request)
        => this.Request = request;

    /// <summary>Gets the underlying transfer request.</summary>
    public HttpTransferRequest Request { get; }

    /// <summary>Gets the transfer identifier.</summary>
    public string Identifier => this.Request.Identifier;

    /// <summary>Gets the request URI.</summary>
    public string Uri => this.Request.Uri;

    /// <summary>Gets the transfer type.</summary>
    public TransferType Type => this.Request.Type;


    double percent;
    /// <summary>Gets the percent complete (0.0–1.0), or -1 when not deterministic.</summary>
    public double PercentComplete
    {
        get => this.percent;
        private set => this.Set(ref this.percent, value);
    }


    long bps;
    /// <summary>Gets the current throughput in bytes per second.</summary>
    public long BytesPerSecond
    {
        get => this.bps;
        private set => this.Set(ref this.bps, value);
    }


    bool deterministic;
    /// <summary>Gets a value indicating whether the total transfer size is known.</summary>
    public bool IsDeterministic
    {
        get => this.deterministic;
        private set => this.Set(ref this.deterministic, value);
    }


    long? toTransfer;
    /// <summary>Gets the total bytes to transfer, when known.</summary>
    public long? BytesToTransfer
    {
        get => this.toTransfer;
        private set => this.Set(ref this.toTransfer, value);
    }


    long bytesXfer;
    /// <summary>Gets the number of bytes transferred so far.</summary>
    public long BytesTransferred
    {
        get => this.bytesXfer;
        private set => this.Set(ref this.bytesXfer, value);
    }


    TimeSpan timeRemaining;
    /// <summary>Gets the estimated time remaining based on current throughput.</summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get => this.timeRemaining;
        private set => this.Set(ref this.timeRemaining, value);
    }


    HttpTransferState status = HttpTransferState.Pending;
    /// <summary>Gets the current transfer state.</summary>
    public HttpTransferState Status
    {
        get => this.status;
        private set => this.Set(ref this.status, value);
    }


    internal void Update(HttpTransferResult result)
    {
        this.Status = result.Status;
        this.IsDeterministic = result.IsDeterministic;

        this.EstimatedTimeRemaining = result.Progress.EstimatedTimeRemaining;
        this.BytesToTransfer = result.Progress.BytesToTransfer;
        this.BytesTransferred = result.Progress.BytesTransferred;
        this.BytesPerSecond = result.Progress.BytesPerSecond;
        this.PercentComplete = result.Progress.PercentComplete;
    }
}
