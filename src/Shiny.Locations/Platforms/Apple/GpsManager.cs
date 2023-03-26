using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Shiny.Locations;


public class GpsManager : NotifyPropertyChanged, IGpsManager, IShinyStartupTask
{
    readonly Subject<GpsReading> readingSubj = new();
    readonly Lazy<IEnumerable<IGpsDelegate>> delegates;
    readonly CLLocationManager locationManager;
    readonly ILogger logger;


    public GpsManager(IServiceProvider services, ILogger<IGpsManager> logger)
    {
        this.delegates = services.GetLazyService<IEnumerable<IGpsDelegate>>();
        this.logger = logger;
        this.locationManager = new CLLocationManager { Delegate = new GpsManagerDelegate(this) };
    }


    internal async void LocationsUpdated(CLLocation[] locations)
    {
        var reading = locations.Last().FromNative();
        await this.delegates
            .Value
            .RunDelegates(x => x.OnReading(reading), this.logger)
            .ConfigureAwait(false);

        this.readingSubj.OnNext(reading);
    }
    internal void OnFailed(NSError error) {}


    public async void Start()
    {
        if (this.CurrentListener != null)
        {
            try
            {
                // only auto-start if auth status was changed to FULL authorized, not restricted
                if (this.locationManager.AuthorizationStatus == CLAuthorizationStatus.Authorized)
                    await this.StartListenerInternal(this.CurrentListener);
                else
                    this.logger.LogInformation("User has removed location permissions");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error trying to restart GPS");
            }
        }
    }


    public IObservable<GpsReading> WhenReading() => this.readingSubj;


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


    AppleGpsRequest? currentSettings;
    public AppleGpsRequest? CurrentSettings
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
                this.Set(ref this.currentSettings, null);
                this.currentSettings = value;
            }
        }
    }


    public GpsRequest? CurrentListener => this.currentSettings;


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
        this.locationManager.AllowsBackgroundLocationUpdates = false;
        this.locationManager.StopUpdatingLocation();
        this.CurrentSettings = null;

        return Task.CompletedTask;
    }


    protected virtual async Task StartListenerInternal(GpsRequest request)
    {
        (await this.RequestAccess(request).ConfigureAwait(false)).Assert();

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

        var bg = request.BackgroundMode != GpsBackgroundMode.None;
        this.locationManager.AllowsBackgroundLocationUpdates = bg;
        this.locationManager.PausesLocationUpdatesAutomatically = false;

        var useSignificant = false;
        if (request is AppleGpsRequest appleRequest)
        {
            this.locationManager.PausesLocationUpdatesAutomatically = appleRequest.PausesLocationUpdatesAutomatically;
            this.locationManager.ShowsBackgroundLocationIndicator = bg && appleRequest.ShowsBackgroundLocationIndicator;
            useSignificant = appleRequest.UseSignificantLocationChanges;

            if (appleRequest.ActivityType != null)
                this.locationManager.ActivityType = appleRequest.ActivityType.Value;

            this.CurrentSettings = appleRequest;
        }
        else
        {
            this.CurrentSettings = new AppleGpsRequest(
                BackgroundMode: request.BackgroundMode,
                Accuracy: request.Accuracy
            );
        }
        //this.locationManager.ShouldDisplayHeadingCalibration = true;
        //this.locationManager.AllowDeferredLocationUpdatesUntil
        if (useSignificant)
            this.locationManager.StartMonitoringSignificantLocationChanges();
        else
            this.locationManager.StartUpdatingLocation();
    }
}
