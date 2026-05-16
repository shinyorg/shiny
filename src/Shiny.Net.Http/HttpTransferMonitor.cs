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
/// This class is used to monitor a list of transfers within your user interface
/// </summary>
public class HttpTransferMonitor(
    IHttpTransferManager manager,
    IRepository repository,
    ILogger<HttpTransferMonitor> logger
) : IDisposable
{
    IDisposable? repoSub;
    IDisposable? updateSub;


    readonly BindingList<HttpTransferObject> transfers = new();
    public INotifyReadOnlyCollection<HttpTransferObject> Transfers => this.transfers;

    public bool IsStarted => this.repoSub != null || this.updateSub != null;


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


    public async Task Start(
        bool removeFinished = true,
        bool removeErrors = true,
        bool removeCancelled = true,
        SynchronizationContext? syncContext = null
    )
    {
        if (this.repoSub != null)
            throw new InvalidOperationException("Already running monitor");

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


    public void Stop()
    {
        this.repoSub?.Dispose();
        this.updateSub?.Dispose();
        this.repoSub = null;
        this.updateSub = null;
    }

    public void Dispose() => this.Stop();
}


public class HttpTransferObject : NotifyPropertyChanged
{
    public HttpTransferObject(HttpTransferRequest request)
        => this.Request = request;

    public HttpTransferRequest Request { get; }
    public string Identifier => this.Request.Identifier;
    public string Uri => this.Request.Uri;
    public TransferType Type => this.Request.Type;


    double percent;
    public double PercentComplete
    {
        get => this.percent;
        private set => this.Set(ref this.percent, value);
    }


    long bps;
    public long BytesPerSecond
    {
        get => this.bps;
        private set => this.Set(ref this.bps, value);
    }


    bool deterministic;
    public bool IsDeterministic
    {
        get => this.deterministic;
        private set => this.Set(ref this.deterministic, value);
    }


    long? toTransfer;
    public long? BytesToTransfer
    {
        get => this.toTransfer;
        private set => this.Set(ref this.toTransfer, value);
    }


    long bytesXfer;
    public long BytesTransferred
    {
        get => this.bytesXfer;
        private set => this.Set(ref this.bytesXfer, value);
    }


    TimeSpan timeRemaining;
    public TimeSpan EstimatedTimeRemaining
    {
        get => this.timeRemaining;
        private set => this.Set(ref this.timeRemaining, value);
    }


    HttpTransferState status = HttpTransferState.Pending;
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
