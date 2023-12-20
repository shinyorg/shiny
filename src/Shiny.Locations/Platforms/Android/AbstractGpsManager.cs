using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P = Android.Manifest.Permission;

namespace Shiny.Locations;


public abstract class AbstractGpsManager : NotifyPropertyChanged, IGpsManager, IShinyStartupTask
{
    readonly Subject<GpsReading> readingSubj;
    readonly ILogger logger;


    protected AbstractGpsManager(AndroidPlatform platform, ILogger logger)
    {
        this.readingSubj = new();
        this.Platform = platform;
        this.logger = logger;
        this.Callback = new ShinyLocationCallback
        {
            OnReading = x => this.readingSubj.OnNext(x.FromNative())
        };
    }


    public virtual async void Start()
    {
        if (this.CurrentSettings != null)
        {
            try
            {
                await this.StartListenerInternal(this.CurrentSettings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to auto-start GPS");
                this.CurrentSettings = null; // remove the settings since it can't be autostarted
            }
        }
    }


    protected ShinyLocationCallback Callback { get; }
    protected AndroidPlatform Platform { get; }

    AndroidGpsRequest? currentSettings;
    public AndroidGpsRequest? CurrentSettings
    {
        get => this.currentSettings;
        set
        {
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            if (bg != GpsBackgroundMode.None)
            {
                this.Set(ref this.currentSettings, value);
            }
            else
            {
                this.Set(ref this.currentSettings, null); // don't track across app restarts
                this.currentSettings = value;
            }
        }
    }


    public GpsRequest? CurrentListener => this.currentSettings;


    public AccessState GetCurrentStatus(GpsRequest request)
    {
        var ps = this.GetPermissionSet(request);
        var states = ps.Select(this.Platform.GetCurrentPermissionStatus).ToList();
        if (states.Any(x => x == AccessState.Unknown))
            return AccessState.Unknown;

        if (states.All(x => x == AccessState.Available))
            return AccessState.Available;

        // TODO: what if fine is denied but coarse is good?  should be restricted
        return AccessState.Denied;
    }


    protected virtual List<string> GetPermissionSet(GpsRequest request)
    {
        var realtime = request.BackgroundMode == GpsBackgroundMode.Realtime;
        var requestBg = false;
        var permissionSet = new List<string> { P.AccessCoarseLocation };
        if (request.Accuracy > GpsAccuracy.Low)
            permissionSet.Add(P.AccessFineLocation);

        switch (request.BackgroundMode)
        {
            case GpsBackgroundMode.Standard:
                // just always request BG
                requestBg = !realtime && OperatingSystemShim.IsAndroidVersionAtLeast(29);
                break;

            case GpsBackgroundMode.Realtime:
                // just always request BG
                requestBg = !realtime && OperatingSystemShim.IsAndroidVersionAtLeast(29);

                if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
                    permissionSet.Add(P.ForegroundService);

                if (OperatingSystemShim.IsAndroidVersionAtLeast(33))
                    permissionSet.Add(P.PostNotifications);

                if (OperatingSystemShim.IsAndroidVersionAtLeast(34))
                    permissionSet.Add(AndroidPermissions.ForegroundServiceLocation);
                break;
        }
        if (requestBg && OperatingSystemShim.IsAndroidVersionAtLeast(29))
            permissionSet.Add(P.AccessBackgroundLocation);

        return permissionSet;
    }

    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var permissionSet = this.GetPermissionSet(request);
        var status = AccessState.Denied;
        var requestBg = permissionSet.Contains(P.AccessBackgroundLocation);

        // TODO: test BG permission shouldn't 
        var result = await this.Platform.RequestPermissions(permissionSet.ToArray()).ToTask();

        if (result.IsGranted(P.AccessCoarseLocation))
        {
            status = AccessState.Available;

            if (permissionSet.Contains(P.AccessFineLocation) && !result.IsGranted(P.AccessFineLocation))
            {
                status = AccessState.Restricted;
            }

            if (requestBg)
            {
                // bg permission must be requested independently from others
                var bgResult = await this.Platform.RequestAccess(P.AccessBackgroundLocation).ToTask();
                if (bgResult != AccessState.Available)
                    status = AccessState.Restricted;
            }

            if (permissionSet.Contains(P.ForegroundService) && !result.IsGranted(P.ForegroundService))
                return AccessState.NotSetup;
        }

        //if (this.Platform.GetSystemService<LocationManager>(Context.LocationService)!.IsLocationEnabled)
        return status;
    }


    public virtual IObservable<GpsReading> WhenReading()
        => this.readingSubj;


    public virtual async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new InvalidOperationException("There is already a GPS listener running");

        await this.StartListenerInternal(request);
    }


    public virtual async Task StopListener()
    {
        if (this.CurrentListener == null)
            return;

        await this.RemoveLocationUpdates();
        if (this.CurrentListener.BackgroundMode == GpsBackgroundMode.Realtime && ShinyGpsService.IsStarted)
            this.Platform.StopService(typeof(ShinyGpsService));

        this.CurrentSettings = null;
    }


    public abstract IObservable<GpsReading?> GetLastReading();
    protected abstract Task RequestLocationUpdates(GpsRequest request);
    protected abstract Task RemoveLocationUpdates();


    protected async Task StartListenerInternal(GpsRequest request)
    {
        request ??= new GpsRequest();
        if (request is not AndroidGpsRequest android)
            android = new AndroidGpsRequest(request.BackgroundMode, request.Accuracy, request.DistanceFilterMeters);

        (await this.RequestAccess(request)).Assert(allowRestricted: true);

        if (request.BackgroundMode == GpsBackgroundMode.Realtime && !ShinyGpsService.IsStarted)
            this.Platform.StartService(typeof(ShinyGpsService), android.StopForegroundServiceWithTask);

        await this.RequestLocationUpdates(request);
        this.CurrentSettings = android;
    }
}
