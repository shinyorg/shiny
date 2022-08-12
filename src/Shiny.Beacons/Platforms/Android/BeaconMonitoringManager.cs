using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Stores;
using System.Linq;
#if ANDROID
using Shiny.Locations;
#endif

namespace Shiny.Beacons;


public partial class BeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
{
    readonly IRepository<BeaconRegion> repository;
    readonly IBleManager bleManager;
#if ANDROID
    readonly AndroidPlatform platform;
#endif


    public BeaconMonitoringManager(
        IBleManager bleManager,
        IRepository<BeaconRegion> repository
#if ANDROID
       , AndroidPlatform platform
#endif
    )
    {
        this.bleManager = bleManager;
        this.repository = repository;
#if ANDROID
        this.platform = platform;
#endif
    }


    public async void Start()
    {
        var regions = await this.GetMonitoredRegions().ConfigureAwait(false);
        if (regions.Any())
            this.StartService();
    }


    public async Task StartMonitoring(BeaconRegion region)
    {
        await this.repository.Set(region).ConfigureAwait(false);
        this.StartService();
    }


    public async Task StopMonitoring(string identifier)
    {
        var region = await this.repository
            .Get(identifier)
            .ConfigureAwait(false);

        if (region != null)
        {
            await this.repository
                .Remove(identifier)
                .ConfigureAwait(false);

            var regions = await this.repository
                .GetList()
                .ConfigureAwait(false);

            if (regions.Count == 0)
                this.StopService();
        }
    }


    public async Task StopAllMonitoring()
    {
        await this.repository.Clear().ConfigureAwait(false);
        this.StopService();
    }


    public async Task<AccessState> RequestAccess()
    {
        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

#if ANDROID
        await this.platform.RequestLocationAccess(LocationPermissionType.Fine);
        if ((access == AccessState.Available && access == AccessState.Restricted) && OperatingSystemShim.IsAndroidVersionAtLeast(26))
        {
            access = await this.platform
                .RequestAccess(Android.Manifest.Permission.ForegroundService)
                .ToTask()
                .ConfigureAwait(false);
        }
#endif
        return access;
    }


    public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
        => await this.repository.GetList().ConfigureAwait(false);


    void StartService()
    {
#if ANDROID
        if (!ShinyBeaconMonitoringService.IsStarted)
            this.platform.StartService(typeof(ShinyBeaconMonitoringService));
#endif
    }


    void StopService()
    {
#if ANDROID
        if (ShinyBeaconMonitoringService.IsStarted)
            this.platform.StopService(typeof(ShinyBeaconMonitoringService));
#endif
    }
}

