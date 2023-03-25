using System;
using System.Collections.Generic;
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
        if (this.CurrentListener != null)
        {
            try
            {
                await this.StartListener(this.CurrentListener).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning("Failed to auto-start GPS", ex);
            }
        }
    }


    protected ShinyLocationCallback Callback { get; }
    protected AndroidPlatform Platform { get; }


    GpsRequest? request;
    public GpsRequest? CurrentListener
    {
        get => this.request;
        set
        {
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            if (bg != GpsBackgroundMode.None)
            {
                this.Set(ref this.request, value);
            }
            else
            {
                this.Set(ref this.request, null); // don't track across app restarts
                this.request = value;
            }
        }
    }


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var status = AccessState.Denied;
        var requestBg = false;
        var permissionSet = new List<string> { P.AccessCoarseLocation };
        if (request.Accuracy > GpsAccuracy.Low)
            permissionSet.Add(P.AccessFineLocation);

        switch (request.BackgroundMode)
        {
            case GpsBackgroundMode.Standard:
                requestBg = OperatingSystemShim.IsAndroidVersionAtLeast(29);
                break;

            case GpsBackgroundMode.Realtime:
                if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
                    permissionSet.Add(P.ForegroundService);

                //if (OperatingSystemShim.IsAndroidVersionAtLeast(33))
                //    permissionSet.Add(AndroidPermissions.PostNotifications);
                break;
        }

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

            // foreground does not fail - the notification will not show but the service will show up in the task manager as per: https://developer.android.com/develop/ui/views/notifications/notification-permission
            //if (permissionSet.Contains(AndroidPermissions.PostNotifications) && !result.IsGranted(AndroidPermissions.PostNotifications))
            //    status = AccessState.Denied; // without post notifications, foreground service will fail
        }

        //if (this.Platform.GetSystemService<LocationManager>(Context.LocationService)!.IsLocationEnabled)
        return status;
    }


    public virtual IObservable<GpsReading> WhenReading()
        => this.readingSubj;


    public virtual async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            return;

        request ??= new GpsRequest();
        (await this.RequestAccess(request)).Assert(allowRestricted: true);

        if (request.BackgroundMode == GpsBackgroundMode.Realtime && !ShinyGpsService.IsStarted)
            this.Platform.StartService(typeof(ShinyGpsService));

        await this.RequestLocationUpdates(request);
        this.CurrentListener = request;
    }


    public virtual async Task StopListener()
    {
        if (this.CurrentListener == null)
            return;

        await this.RemoveLocationUpdates();
        if (this.CurrentListener.BackgroundMode == GpsBackgroundMode.Realtime && ShinyGpsService.IsStarted)
            this.Platform.StopService(typeof(ShinyGpsService));

        this.CurrentListener = null;
    }


    public abstract IObservable<GpsReading?> GetLastReading();
    protected abstract Task RequestLocationUpdates(GpsRequest request);
    protected abstract Task RemoveLocationUpdates();
}
