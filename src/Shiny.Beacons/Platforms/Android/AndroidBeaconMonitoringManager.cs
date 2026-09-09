using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;
using Shiny.Extensions.Stores.Repositories;
using P = Android.Manifest.Permission;

namespace Shiny.Beacons;


/// <summary>
/// Beacon region monitoring on Android: the shared scan engine, kept alive by a foreground service.
/// </summary>
/// <remarks>
/// Android has no OS-level beacon region API, so monitoring is the same BLE scan ranging uses, run
/// at low power with the enter/exit state machine on top. The only way to keep that scan running
/// once the app leaves the foreground is a foreground service with a visible notification.
/// </remarks>
public class AndroidBeaconMonitoringManager : BleScanBeaconMonitoringManager, IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly ILogger logger;


    /// <summary>
    /// Creates the manager.
    /// </summary>
    public AndroidBeaconMonitoringManager(
        IBleManager bleManager,
        IRepository repository,
        IServiceProvider services,
        AndroidPlatform platform,
        BeaconRangingOptions options,
        ILogger<IBeaconMonitoringManager> logger
    ) : base(bleManager, repository, services, options, logger)
    {
        this.platform = platform;
        this.logger = logger;
    }


    /// <inheritdoc />
    public void Start()
    {
        try
        {
            // regions survive a restart, so monitoring has to come back up with the app
            if (this.GetMonitoredRegions().Count > 0)
                this.StartService();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to restart beacon monitoring");
        }
    }


    /// <inheritdoc />
    public override async Task<AccessState> RequestAccess()
    {
        var access = await base.RequestAccess().ConfigureAwait(false);
        if (access != AccessState.Available)
            return access;

        var result = await this.platform
            .RequestFilteredPermissions(
                new(P.ForegroundServiceConnectedDevice, 34, null),
                // not required - asked for so the service's own notification is actually visible
                new(P.PostNotifications, 33, null)
            )
            .ConfigureAwait(false);

        return result.IsSuccess() ? AccessState.Available : AccessState.Denied;
    }


    /// <inheritdoc />
    public override async Task StartMonitoring(BeaconRegion region)
    {
        await base.StartMonitoring(region).ConfigureAwait(false);
        this.StartService();
    }


    /// <inheritdoc />
    public override async Task StopMonitoring(string identifier)
    {
        await base.StopMonitoring(identifier).ConfigureAwait(false);

        if (this.GetMonitoredRegions().Count == 0)
            this.StopService();
    }


    /// <inheritdoc />
    public override async Task StopAllMonitoring()
    {
        await base.StopAllMonitoring().ConfigureAwait(false);
        this.StopService();
    }


    /// <summary>
    /// Called by the foreground service once it is running, to bring the scan up inside it.
    /// </summary>
    internal void OnServiceStarted() => _ = this.StartScan();

    /// <summary>
    /// Called by the foreground service as it stops.
    /// </summary>
    internal void OnServiceStopped() => _ = this.StopScan();


    void StartService()
    {
        if (!ShinyBeaconMonitoringService.IsStarted)
            this.platform.StartService(typeof(ShinyBeaconMonitoringService));
    }


    void StopService()
    {
        if (ShinyBeaconMonitoringService.IsStarted)
            this.platform.StopService(typeof(ShinyBeaconMonitoringService));
    }
}
