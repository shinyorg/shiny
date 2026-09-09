using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Collections;

namespace Shiny.Beacons.Managed;


/// <summary>
/// Maintains a live, bindable list of the beacons currently in range of a region.
/// </summary>
/// <remarks>
/// Ranging emits an observation per advertisement, which is far too chatty to bind a UI to
/// directly. This coalesces those into one entry per beacon, updates it in place, and drops
/// entries that have gone quiet.
/// </remarks>
public class ManagedBeaconScan(IBeaconRangingManager rangingManager) : IDisposable
{
    readonly BindingList<ManagedBeacon> beacons = new();
    CompositeDisposable? disposer;
    IScheduler? scheduler;


    /// <summary>The beacons currently in range, newest additions last.</summary>
    public INotifyReadOnlyCollection<ManagedBeacon> Beacons => this.beacons;

    /// <summary>The region being scanned, or null when the scan is stopped.</summary>
    public BeaconRegion? ScanningRegion { get; private set; }

    /// <summary>Whether a scan is running.</summary>
    public bool IsScanning => this.ScanningRegion != null;

    /// <summary>How long a beacon stays in the list after it was last seen. Null keeps them forever.</summary>
    public TimeSpan? ClearTime { get; private set; }

    /// <summary>How long observations are batched for before the list is updated.</summary>
    public TimeSpan BufferTime { get; private set; } = TimeSpan.FromSeconds(2);


    /// <summary>
    /// Starts scanning a region.
    /// </summary>
    /// <param name="region">The region to range.</param>
    /// <param name="scheduler">
    /// The scheduler list updates are marshalled onto - pass the UI scheduler when binding.
    /// </param>
    /// <param name="bufferTime">How long to batch observations for. Defaults to 2 seconds.</param>
    /// <param name="clearTime">
    /// How long a beacon stays listed after its last sighting. Defaults to no expiry.
    /// </param>
    public async Task Start(
        BeaconRegion region,
        IScheduler? scheduler = null,
        TimeSpan? bufferTime = null,
        TimeSpan? clearTime = null
    )
    {
        if (this.IsScanning)
            throw new InvalidOperationException("A beacon scan is already running");

        (await rangingManager.RequestAccess().ConfigureAwait(false)).Assert();

        this.scheduler = scheduler;
        this.BufferTime = bufferTime ?? TimeSpan.FromSeconds(2);
        this.ClearTime = clearTime;
        this.ScanningRegion = region;
        this.beacons.Clear();
        this.disposer = new CompositeDisposable();

        this.disposer.Add(
            rangingManager
                .WhenBeaconRanged(region)
                .Buffer(this.BufferTime)
                .Where(x => x.Count > 0)
                .ObserveOnIf(scheduler)
                .Subscribe(this.OnBeacons)
        );

        if (clearTime != null)
        {
            this.disposer.Add(
                Observable
                    .Interval(TimeSpan.FromSeconds(5))
                    .ObserveOnIf(scheduler)
                    .Subscribe(_ => this.RemoveStale(clearTime.Value))
            );
        }
    }


    /// <summary>
    /// Stops the scan. The list is left as it was so a UI does not blank out.
    /// </summary>
    public void Stop()
    {
        this.disposer?.Dispose();
        this.disposer = null;
        this.scheduler = null;
        this.ScanningRegion = null;
    }


    /// <inheritdoc />
    public void Dispose()
    {
        this.Stop();
        this.beacons.Clear();
        GC.SuppressFinalize(this);
    }


    void OnBeacons(IList<Beacon> observations)
    {
        var regionIdentifier = this.ScanningRegion?.Identifier ?? String.Empty;
        var additions = new List<ManagedBeacon>();

        foreach (var observation in observations)
        {
            var managed =
                this.beacons.FirstOrDefault(x => x.Identity.Equals(observation.Identity)) ??
                additions.FirstOrDefault(x => x.Identity.Equals(observation.Identity));

            if (managed == null)
                additions.Add(new ManagedBeacon(observation, regionIdentifier));
            else
                managed.Update(observation);
        }

        if (additions.Count > 0)
            this.beacons.AddRange(additions);
    }


    void RemoveStale(TimeSpan clearTime)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(clearTime);
        var stale = this.beacons.Where(x => x.LastSeen < cutoff).ToList();

        if (stale.Count > 0)
            this.beacons.RemoveRange(stale);
    }
}


/// <summary>
/// Creates managed scans.
/// </summary>
public static class ManagedBeaconScanExtensions
{
    /// <summary>
    /// Marshals onto a scheduler when one was supplied, and leaves the sequence alone when it was not.
    /// </summary>
    /// <remarks>
    /// A UI binds on its own thread, but a headless caller has no scheduler to give and should not
    /// pay for a hop it does not need.
    /// </remarks>
    internal static IObservable<T> ObserveOnIf<T>(this IObservable<T> observable, IScheduler? scheduler)
        => scheduler == null ? observable : observable.ObserveOn(scheduler);


    /// <summary>
    /// Creates a managed scan over this ranging manager.
    /// </summary>
    /// <param name="rangingManager">The ranging manager.</param>
    public static ManagedBeaconScan CreateManagedScan(this IBeaconRangingManager rangingManager)
        => new(rangingManager);
}
