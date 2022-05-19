using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Shiny.Locations;


public partial class GpsManager : IGpsManager, IShinyStartupTask
{
    readonly CLLocationManager locationManager;
    readonly GpsManagerDelegate gdelegate;
    readonly ILogger logger;


    public GpsManager(ILogger<IGpsManager> logger)
    {
        this.logger = logger;
        this.gdelegate = new GpsManagerDelegate();
        this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
    }


    public async void Start()
    {
        if (this.CurrentListener != null)
        {
            try
            {
                // only auto-start if auth status was changed to FULL authorized, not restricted
                if (this.locationManager.AuthorizationStatus == CLAuthorizationStatus.Authorized)
                    await this.StartListenerInternal(this.CurrentListener);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error trying to restart GPS");
            }
        }
    }


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var bg = request.BackgroundMode != GpsBackgroundMode.None;
        var status = await this.locationManager.RequestAccess(bg);

        if (status == AccessState.Available &&
            request.Accuracy > GpsAccuracy.Lowest &&
            UIDevice.CurrentDevice.CheckSystemVersion(14, 0) &&
            this.locationManager.AccuracyAuthorization != CLAccuracyAuthorization.FullAccuracy)
        {
            status = AccessState.Restricted;
        }

        return status;
    }


    public AccessState GetCurrentStatus(GpsRequest request)
        => this.locationManager.GetCurrentStatus(request.BackgroundMode != GpsBackgroundMode.None);


    GpsRequest? request;
    public GpsRequest? CurrentListener
    {
        get => this.request;
        set
        {
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            if (bg == GpsBackgroundMode.None)
                this.request = value;
            else
                this.Set(ref this.request, value);
        }
    }


    public IObservable<GpsReading?> GetLastReading() => Observable.FromAsync<GpsReading?>(async ct =>
    {
        (await this.RequestAccess(GpsRequest.Foreground)).Assert();
        if (this.locationManager.Location == null)
            return null;

        return this.locationManager.Location.FromNative();
    });


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new ArgumentException("There is already an active GPS listener");

        await this.StartListenerInternal(request);
    }


    public Task StopListener()
    {
#if __IOS__
        this.locationManager.AllowsBackgroundLocationUpdates = false;
#endif
        this.locationManager.StopUpdatingLocation();
        this.CurrentListener = null;

        return Task.CompletedTask;
    }


    protected virtual async Task StartListenerInternal(GpsRequest request)
    {
        (await this.RequestAccess(request).ConfigureAwait(false)).Assert();
        this.gdelegate.Request = request;

        switch (request.Accuracy)
        {
            case GpsAccuracy.Highest:
                this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
                break;

            case GpsAccuracy.High:
                this.locationManager.DistanceFilter = 10;
                this.locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
                break;

            case GpsAccuracy.Normal:
                this.locationManager.DistanceFilter = 100;
                this.locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
                break;

            case GpsAccuracy.Low:
                this.locationManager.DistanceFilter = 1000;
                this.locationManager.DesiredAccuracy = CLLocation.AccuracyKilometer;
                break;

            case GpsAccuracy.Lowest:
                this.locationManager.DistanceFilter = 3000;
                this.locationManager.DesiredAccuracy = CLLocation.AccuracyThreeKilometers;
                break;
        }
#if __IOS__
        this.locationManager.AllowsBackgroundLocationUpdates = request.BackgroundMode != GpsBackgroundMode.None;
#endif
        this.locationManager.StartUpdatingLocation();
        this.CurrentListener = request;
    }

    public IObservable<GpsReading> WhenReading() => this.gdelegate.WhenGps();
}
