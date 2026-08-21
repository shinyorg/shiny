using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


/// <summary>
/// Mirrors <see cref="IHttpTransferManager"/> onto whatever progress surfaces are registered - an iOS Live
/// Activity, the Android foreground-service notification, or an app's own renderer.
/// </summary>
/// <remarks>
/// One manager for every platform, deliberately. Aggregation, update coalescing, scope and surface
/// lifetime are the parts worth getting right and the parts that would silently drift if each platform
/// owned a copy, so they live here and an <see cref="ITransferProgressRenderer"/> owns nothing but the
/// drawing.
/// <para>
/// It is an <see cref="IShinyStartupTask"/> rather than something resolved on first use because transfers -
/// and the surfaces representing them - outlive the app. iOS relaunches the process in the background when
/// a background <c>NSURLSession</c> finishes, and the manager has to already be subscribed at that moment
/// to move the surface to its final state.
/// </para>
/// </remarks>
public class TransferProgressManager : IShinyStartupTask, IDisposable
{
    /// <summary>The surface key used by <see cref="TransferProgressScope.Summary"/>.</summary>
    public const string SummaryKey = "summary";

    readonly IHttpTransferManager transfers;
    readonly IReadOnlyList<ITransferProgressRenderer> renderers;
    readonly TransferProgressOptions options;
    readonly ILogger logger;
    readonly ITransferProgressDelegate? progressDelegate;

    readonly ConcurrentDictionary<string, HttpTransferResult> active = new();
    readonly ConcurrentDictionary<string, HttpTransferResult> finished = new();
    readonly Dictionary<string, ThrottleState> throttle = new();
    readonly SemaphoreSlim sync = new(1, 1);
    EventHandler<HttpTransferResult>? handler;


    /// <summary>Creates the manager.</summary>
    /// <param name="transfers">The transfer manager to mirror.</param>
    /// <param name="renderers">Every registered renderer; the unavailable ones are dropped at startup.</param>
    /// <param name="options">The configured behaviour.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="progressDelegate">Optional text overrides.</param>
    public TransferProgressManager(
        IHttpTransferManager transfers,
        IEnumerable<ITransferProgressRenderer> renderers,
        TransferProgressOptions options,
        ILogger<TransferProgressManager> logger,
        ITransferProgressDelegate? progressDelegate = null
    )
    {
        this.transfers = transfers;
        this.options = options;
        this.logger = logger;
        this.progressDelegate = progressDelegate;
        this.renderers = renderers.Where(x => x.IsAvailable).ToList();
    }


    /// <summary>
    /// Subscribes to transfer updates and reconciles surfaces left behind by a previous launch. Invoked by
    /// the Shiny host at startup - do not call it yourself.
    /// </summary>
    public async void Start()
    {
        if (this.renderers.Count == 0)
        {
            this.logger.LogInformation("No transfer progress renderer is available here - transfer progress surfaces are disabled");
            return;
        }

        if (this.handler == null)
        {
            this.handler = (_, result) => this.OnTransferUpdate(result);
            this.transfers.UpdateReceived += this.handler;
        }

        try
        {
            var running = await this.transfers.GetTransfers().ConfigureAwait(false);
            var keys = this.options.Scope == TransferProgressScope.PerTransfer
                ? running.Select(x => x.Identifier).ToList()
                : running.Count == 0 ? [] : new List<string> { SummaryKey };

            foreach (var renderer in this.renderers)
                await renderer.Reconcile(keys).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to reconcile transfer progress surfaces left over from a previous launch");
        }
    }


    /// <summary>Unsubscribes from transfer updates. Anything already on screen is left alone.</summary>
    public void Dispose()
    {
        if (this.handler != null)
        {
            this.transfers.UpdateReceived -= this.handler;
            this.handler = null;
        }
        this.sync.Dispose();
        GC.SuppressFinalize(this);
    }


    void OnTransferUpdate(HttpTransferResult result)
    {
        if (this.options.Scope == TransferProgressScope.PerTransfer)
        {
            this.Push(
                result.Request.Identifier,
                new TransferProgressSnapshot([result], result.Status, result.Progress, IsSummary: false)
            );
            return;
        }

        var id = result.Request.Identifier;
        if (result.Status is HttpTransferState.Completed or HttpTransferState.Error or HttpTransferState.Canceled)
        {
            this.active.TryRemove(id, out _);
            this.finished[id] = result;
        }
        else
        {
            this.active[id] = result;
        }

        var running = this.active.Values.ToList();
        var done = this.finished.Values.ToList();

        if (running.Count > 0)
        {
            // finished transfers stay in the aggregate so the bar never walks backwards when one of a batch
            // completes - only the status comes from what is still moving
            this.Push(
                SummaryKey,
                TransferProgressSnapshot.Aggregate([.. running, .. done], TransferProgressSnapshot.RunningStatus(running))
            );
            return;
        }

        // the batch is done - report how it finished, then retire the surface
        this.finished.Clear();
        if (done.Count > 0)
            this.Push(SummaryKey, TransferProgressSnapshot.Aggregate(done, TransferProgressSnapshot.TerminalStatus(done)));
    }


    /// <summary>
    /// Whether this snapshot is worth drawing, given how recently and how much the last one moved. A state
    /// change, a terminal state, and the first update for a key always pass.
    /// </summary>
    /// <param name="key">The surface key.</param>
    /// <param name="snapshot">The candidate snapshot.</param>
    protected virtual bool ShouldPush(string key, TransferProgressSnapshot snapshot)
    {
        lock (this.throttle)
        {
            if (!this.throttle.TryGetValue(key, out var last))
                return true;

            if (snapshot.IsTerminal || snapshot.Status != last.Status)
                return true;

            var elapsed = DateTimeOffset.UtcNow - last.At;
            if (elapsed < this.options.MinimumUpdateInterval)
                return false;

            // let a refresh through before the content goes stale, even if nothing moved
            if (this.options.StaleAfter is { } stale && elapsed >= stale)
                return true;

            return Math.Abs((snapshot.Fraction ?? 0d) - last.Fraction) >= this.options.MinimumPercentChange;
        }
    }


    void Push(string key, TransferProgressSnapshot snapshot)
    {
        if (!this.ShouldPush(key, snapshot))
            return;

        lock (this.throttle)
        {
            if (snapshot.IsTerminal)
                this.throttle.Remove(key);
            else
                this.throttle[key] = new(DateTimeOffset.UtcNow, snapshot.Status, snapshot.Fraction ?? 0d);
        }
        _ = this.PushSerialized(key, snapshot);
    }


    async Task PushSerialized(string key, TransferProgressSnapshot snapshot)
    {
        await this.sync.WaitAsync().ConfigureAwait(false);
        try
        {
            var content = TransferProgressContentBuilder.Build(snapshot, this.options, this.progressDelegate);
            var dismissAt = DateTimeOffset.UtcNow.Add(this.options.DismissCompletedAfter);

            foreach (var renderer in this.renderers)
            {
                try
                {
                    if (snapshot.IsTerminal)
                        await renderer.Hide(key, content, dismissAt).ConfigureAwait(false);
                    else
                        await renderer.Show(key, content).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // one renderer failing must never take out the others, and never the transfer pipeline
                    this.logger.LogWarning(ex, "Transfer progress renderer {Renderer} failed for '{Key}'", renderer.GetType().Name, key);
                }
            }
        }
        finally
        {
            this.sync.Release();
        }
    }


    readonly record struct ThrottleState(DateTimeOffset At, HttpTransferState Status, double Fraction);
}
