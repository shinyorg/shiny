using System;
using System.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using UIKit;

namespace Shiny.Locations;


public class CLLocationGpsManager : NotifyPropertyChanged, IGpsManager, IShinyStartupTask
{
    const string CurrentSettingsKey = "Shiny.Locations.GpsManager.CurrentSettings";

    readonly StationaryDetector stationaryDetector = new();
    readonly IServiceProvider services;
    readonly CLLocationManager locationManager;
    readonly ILogger logger;
    readonly IKeyValueStore store;


    public CLLocationGpsManager(IServiceProvider services, IKeyValueStore store, ILogger<IGpsManager> logger)
    {
        this.services = services;
        this.store = store;
        this.logger = logger;
        this.locationManager = new CLLocationManager
        {
            Delegate = new GpsManagerDelegate(this)
        };
        this.currentSettings = store.Get<AppleGpsRequest>(CurrentSettingsKey);
    }


    public event EventHandler<GpsReading>? GpsReadingReceived;


    internal async void LocationsUpdated(CLLocation[] locations)
    {
        if (this.CurrentSettings == null)
            return;

        var reading = locations.Last().FromNative();
        var isStationary = this.stationaryDetector.Detect(
            reading,
            this.CurrentSettings.StationaryMetersThreshold,
            this.CurrentSettings.StationarySecondsThreshold
        );
        if (isStationary)
            reading = reading with { IsStationary = true };

        await this.services
            .RunDelegates<IGpsDelegate>(
                x => x.OnReading(reading),
                this.logger
            )
            .ConfigureAwait(false);

        this.GpsReadingReceived?.Invoke(this, reading);
    }

    internal void OnFailed(NSError error) {}


    public async void Start()
    {
        if (this.CurrentSettings is not { AutoRestart: true })
        {
            this.CurrentSettings = null;
            return;
        }
        try
        {
            // only auto-start if auth status was changed to FULL authorized, not restricted
            if (this.locationManager.AuthorizationStatus == CLAuthorizationStatus.Authorized)
            {
                if (this.CurrentSettings != null)
                    await this.StartListenerInternal(this.CurrentSettings);
            }
            else
            {
                this.logger.LogInformation("User has removed location permissions");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error trying to restart GPS");
        }
    }


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var status = await this.locationManager.RequestAccess(request.BackgroundMode != GpsBackgroundMode.None);

        if (
            status == AccessState.Available &&
            request.RequestPreciseAccuracy &&
            UIDevice.CurrentDevice.CheckSystemVersion(14, 0) &&
            this.locationManager.AccuracyAuthorization == CLAccuracyAuthorization.ReducedAccuracy
        )
        {
            await this.locationManager.RequestTemporaryFullAccuracyAuthorizationAsync("shinygps");

            status = this.locationManager.AccuracyAuthorization == CLAccuracyAuthorization.ReducedAccuracy
                ? AccessState.Restricted
                : AccessState.Available;
        }

        return status;
    }


    public AccessState GetCurrentStatus(GpsRequest request)
        => this.locationManager.GetCurrentStatus(
            request.BackgroundMode != GpsBackgroundMode.None,
            request.RequestPreciseAccuracy
        );


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
            this.store.SetOrRemove(CurrentSettingsKey, value);
        }
    }


    public GpsRequest? CurrentListener => this.currentSettings;


    public Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        if (this.locationManager.Location == null)
            return Task.FromResult<GpsReading?>(null);

        return Task.FromResult<GpsReading?>(this.locationManager.Location.FromNative());
    }


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new ArgumentException("There is already an active GPS listener");

        await this.StartListenerInternal(request);
    }


    public Task StopListener()
    {
        if (this.CurrentSettings == null)
            return Task.CompletedTask;

        if (this.CurrentSettings.BackgroundMode == GpsBackgroundMode.Standard)
            this.locationManager.StopMonitoringSignificantLocationChanges();
        else
            this.locationManager.StopUpdatingLocation();

        this.locationManager.AllowsBackgroundLocationUpdates = false;
        this.CurrentSettings = null;
        return Task.CompletedTask;
    }


    protected virtual async Task StartListenerInternal(GpsRequest request)
    {
        // this.locationManager.DistanceFilter = request.DistanceFilterMeters;
        // this.locationManager.DesiredAccuracy = request.Accuracy switch
        // {
        //     //CLLocation.AccurracyBestForNavigation;
        //     //CLLocation.AccuracyReduced
        //     GpsAccuracy.Highest => CLLocation.AccuracyBest,
        //     GpsAccuracy.High => CLLocation.AccuracyNearestTenMeters,
        //     GpsAccuracy.Normal => CLLocation.AccuracyHundredMeters,
        //     GpsAccuracy.Low => CLLocation.AccuracyKilometer,
        //     GpsAccuracy.Lowest => CLLocation.AccuracyThreeKilometers
        // };

        // this.locationManager.UpdatedHeading
        // this.locationManager.DeferredUpdatesFinished
        // this.locationManager.ShouldDisplayHeadingCalibration
        // this.locationManager.DismissHeadingCalibrationDisplay();
        // this.locationManager.Heading
        // this.locationManager.AccuracyAuthorization
        // if (this.locationManager.StopUpdatingHeading();
        // this.locationManager.StartUpdatingHeading();

        var appleRequest = request.ToApple();
        this.locationManager.PausesLocationUpdatesAutomatically = appleRequest.PausesLocationUpdatesAutomatically;
        this.locationManager.ActivityType = appleRequest.ActivityType;

        switch (request.BackgroundMode)
        {
            case GpsBackgroundMode.None:
                this.locationManager.StartUpdatingLocation();
                break;

            case GpsBackgroundMode.Standard:
                this.locationManager.AllowsBackgroundLocationUpdates = true;
                this.locationManager.ShowsBackgroundLocationIndicator = appleRequest.ShowsBackgroundLocationIndicator;
                this.locationManager.StartMonitoringSignificantLocationChanges();
                break;

            case GpsBackgroundMode.Realtime:
                this.locationManager.AllowsBackgroundLocationUpdates = true;
                this.locationManager.ShowsBackgroundLocationIndicator = appleRequest.ShowsBackgroundLocationIndicator;
                this.locationManager.StartUpdatingLocation();
                break;
        }
        this.CurrentSettings = appleRequest;
    }
}
