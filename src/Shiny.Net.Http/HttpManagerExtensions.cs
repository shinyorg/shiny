using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public class ManagedList : IDisposable
{
    readonly Action dispose;


    internal ManagedList(Action disposeAction, ObservableList<HttpTransferObject> transfers)
    {
        this.dispose = disposeAction;
        this.Transfers = transfers;
    }


    public INotifyReadOnlyCollection<HttpTransferObject> Transfers { get; }
    public void Dispose() => this.dispose.Invoke();
}


public class HttpTransferObject : NotifyPropertyChanged
{
    public HttpTransferObject(HttpTransferRequest request)
        => this.Request = request;

    public HttpTransferRequest Request { get; }
    public string Identifier => this.Request.Identifier;
    public string Uri => this.Request.Uri;
    public bool IsUpload => this.Request.IsUpload;


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


public static class HttpManagerExtensions
{
    public static async Task<ManagedList> CreateManagedList(
        this IHttpTransferManager manager,
        bool removeFinished = true,
        bool removeErrors = true,
        IScheduler? scheduler = null
    )
    {
        // TODO: I need to watch the repository for adds - the observable won't fire until traffic is placed on it
        var list = new ObservableList<HttpTransferObject>();
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
                    )
                ));
                return obj;
            });

        var sub = manager
            .WhenUpdateReceived()
            .ObserveOnIf(scheduler)
            .Subscribe(x =>
            {
                // sync lock the collection?
                var item = list.FirstOrDefault(y => y.Identifier.Equals(x.Request.Identifier));

                if (x.Status == HttpTransferState.Completed && removeFinished)
                {
                    if (item != null)
                        list.Remove(item);
                }
                else if (x.Status == HttpTransferState.Error && removeErrors)
                {
                    if (item != null)
                        list.Remove(item);
                }
                else
                {
                    if (item == null)
                    {
                        item = new HttpTransferObject(x.Request);
                        list.Add(item);
                    }
                    item.Update(x);
                }
            });

        return new ManagedList(() => sub.Dispose(), list);
    }
}

