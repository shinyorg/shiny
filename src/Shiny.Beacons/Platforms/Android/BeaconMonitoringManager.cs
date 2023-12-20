using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;
using Shiny.Support.Repositories;
using P = Android.Manifest.Permission;

namespace Shiny.Beacons;


public partial class BeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
{
    readonly IRepository repository;
    readonly IBleManager bleManager;
    readonly AndroidPlatform platform;
    readonly ILogger logger;


    public BeaconMonitoringManager(
        IBleManager bleManager,
        IRepository repository,
        AndroidPlatform platform,
        ILogger<BeaconMonitoringManager> logger
    )
    {
        this.bleManager = bleManager;
        this.repository = repository;
        this.platform = platform;
        this.logger = logger;
    }


    public void Start()
    {
        try
        {
            var regions = this.GetMonitoredRegions();
            if (regions.Any())
                this.StartService();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to start beacon monitoring");
        }
    }


    public async Task StartMonitoring(BeaconRegion region)
    {
        (await this.RequestAccess()).Assert();

        this.repository.Set(region);
        this.StartService();
    }


    public void StopMonitoring(string identifier)
    {
        var region = this.repository.Get<BeaconRegion>(identifier);

        if (region != null)
        {
            this.repository.Remove<BeaconRegion>(identifier);
            var regions = this.repository.GetList<BeaconRegion>();

            if (regions.Count == 0)
                this.StopService();
        }
    }


    public void StopAllMonitoring()
    {
        this.repository.Clear<BeaconRegion>();
        this.StopService();
    }


    public async Task<AccessState> RequestAccess()
    {
        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

        if (access == AccessState.Available)
        {
            var result = await this.platform
                .RequestFilteredPermissions(
                    new(P.ForegroundService, 31, null),
                    new(P.PostNotifications, 33, null) // this isn't required - we ask to be nice
                )
                .ToTask();

            if (!result.IsSuccess())
                access = AccessState.Denied;
        }
        return access;
    }


    public IList<BeaconRegion> GetMonitoredRegions()
        => this.repository.GetList<BeaconRegion>();


    void StartService()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(29) && !ShinyBeaconMonitoringService.IsStarted)
            this.platform.StartService(typeof(ShinyBeaconMonitoringService));
    }


    void StopService()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(29) && ShinyBeaconMonitoringService.IsStarted)
            this.platform.StopService(typeof(ShinyBeaconMonitoringService));
    }
}

