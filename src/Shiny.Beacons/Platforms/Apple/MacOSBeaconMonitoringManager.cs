#if MACOS
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Reports that beacon region monitoring is unavailable on macOS.
/// </summary>
/// <remarks>
/// <para>
/// CoreLocation on the Mac binds <c>CLBeaconIdentityConstraint</c> and supports ranging, but it has
/// never had beacon region monitoring - there is no <c>CLServiceSession</c> to hold a background
/// authorization, and <c>startMonitoring(for: CLBeaconRegion)</c> is not available.
/// </para>
/// <para>
/// This is registered rather than left unregistered so that startup code shared with iOS still
/// compiles and runs on macOS. It reports <see cref="AccessState.NotSupported"/> through the same
/// channel every other capability uses, so a caller that checks access before monitoring behaves
/// correctly without a macOS-specific branch. Ranging and Eddystone scanning both work normally.
/// </para>
/// </remarks>
public class MacOSBeaconMonitoringManager : IBeaconMonitoringManager
{
    const string Message = "macOS does not support beacon region monitoring - CoreLocation has no beacon region API on the Mac. Use IBeaconRangingManager while your app is running, or IEddystoneScanner.";

    /// <inheritdoc />
    public AccessState CurrentStatus => AccessState.NotSupported;

    /// <inheritdoc />
    public Task<AccessState> RequestAccess() => Task.FromResult(AccessState.NotSupported);

    /// <inheritdoc />
    public IList<BeaconRegion> GetMonitoredRegions() => [];

    /// <inheritdoc />
    public Task StartMonitoring(BeaconRegion region) => throw new PlatformNotSupportedException(Message);

    /// <inheritdoc />
    public Task StopMonitoring(string identifier) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAllMonitoring() => Task.CompletedTask;

    /// <inheritdoc />
    public Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default)
        => Task.FromResult(BeaconRegionState.Unknown);
}
#endif
