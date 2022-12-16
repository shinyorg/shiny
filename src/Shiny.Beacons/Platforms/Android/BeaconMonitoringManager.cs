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


    public void Start()
    {
        var regions = this.GetMonitoredRegions();
        if (regions.Any())
            this.StartService();
    }


    public async Task StartMonitoring(BeaconRegion region)
    {
#if ANDROID
        await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);
#endif
        this.repository.Set(region);
        this.StartService();
    }


    public void StopMonitoring(string identifier)
    {
        var region = this.repository.Get(identifier);

        if (region != null)
        {
            this.repository.Remove(identifier);
            var regions = this.repository.GetList();

            if (regions.Count == 0)
                this.StopService();
        }
    }


    public void StopAllMonitoring()
    {
        this.repository.Clear();
        this.StopService();
    }


    public async Task<AccessState> RequestAccess()
    {
        // TODO: fix permission check for iOS
#if ANDROID
        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

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


    public IList<BeaconRegion> GetMonitoredRegions()
        => this.repository.GetList();


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

