using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http.Tests.Fakes;


/// <summary>Records what <see cref="TransferProgressManager"/> asked it to draw.</summary>
public sealed class FakeProgressRenderer(bool available = true) : ITransferProgressRenderer
{
    public bool IsAvailable { get; } = available;

    public List<(string Key, TransferProgressContent Content)> Shown { get; } = new();
    public List<(string Key, TransferProgressContent Content)> Hidden { get; } = new();
    public IReadOnlyCollection<string>? ReconciledWith { get; private set; }

    public Task Show(string key, TransferProgressContent content)
    {
        lock (this.Shown)
            this.Shown.Add((key, content));

        return Task.CompletedTask;
    }

    public Task Hide(string key, TransferProgressContent content, DateTimeOffset dismissAt)
    {
        lock (this.Hidden)
            this.Hidden.Add((key, content));

        return Task.CompletedTask;
    }

    public Task Reconcile(IReadOnlyCollection<string> activeKeys)
    {
        this.ReconciledWith = activeKeys;
        return Task.CompletedTask;
    }
}


/// <summary>A minimal <see cref="IHttpTransferManager"/> the progress tests can drive by hand.</summary>
public sealed class FakeTransferManager : IHttpTransferManager
{
    readonly List<HttpTransfer> transfers = new();

    public event EventHandler<int>? CountChanged;
    public event EventHandler<HttpTransferResult>? UpdateReceived;

    public void Add(params HttpTransfer[] items) => this.transfers.AddRange(items);

    /// <summary>Raises an update exactly as a platform transfer manager would.</summary>
    public void Raise(HttpTransferResult result) => this.UpdateReceived?.Invoke(this, result);

    public bool HasSubscribers => this.UpdateReceived != null;

    public Task<IList<HttpTransfer>> GetTransfers() => Task.FromResult<IList<HttpTransfer>>(this.transfers);

    public Task<HttpTransfer> Queue(HttpTransferRequest request) => throw new NotSupportedException();
    public Task Cancel(string identifier) => Task.CompletedTask;
    public Task Pause(string identifier) => Task.CompletedTask;
    public Task Resume(string identifier) => Task.CompletedTask;
    public Task CancelAll() => Task.CompletedTask;

    void Unused() => this.CountChanged?.Invoke(this, 0);
}
