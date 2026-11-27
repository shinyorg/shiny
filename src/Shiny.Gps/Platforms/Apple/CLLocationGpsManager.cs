using System;
using System.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;

namespace Shiny.Locations;


public class CLLocationGpsManager : IGpsManager, IShinyStartupTask
{
    const string CurrentSettingsKey = "Shiny.Locations.GpsManager.CurrentSettings";

    readonly StationaryDetector stationaryDetector = new();
    readonly IServiceProvider services;
    readonly CLLocationManager locationManager;
    readonly ILogger logger;
    readonly IKeyValueStore store;


    public CLLocationGpsManager(IServiceProvider services, [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store, ILogger<IGpsManager> logger)
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
        // async void from a native callback - nothing may escape or it takes the process down
        try
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
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing GPS reading");
        }
    }

    // CLLocationManager keeps trying after most failures (kCLErrorLocationUnknown is transient), so
    // this only reports - nothing is stopped here
    internal void OnFailed(NSError error)
        => this.logger.LogWarning("CLLocationManager failed: {Error} (code {Code})", error.LocalizedDescription, error.Code);


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
            this.currentSettings = value;
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            this.store.SetOrRemove(CurrentSettingsKey, bg != GpsBackgroundMode.None ? value : null);
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

        if (UsesSignificantChanges(this.CurrentSettings))
            this.locationManager.StopMonitoringSignificantLocationChanges();
        else
            this.locationManager.StopUpdatingLocation();

        this.stationaryDetector.Reset();

#if !MACOS
        this.locationManager.AllowsBackgroundLocationUpdates = false;
#endif
        this.CurrentSettings = null;
        return Task.CompletedTask;
    }


    protected virtual async Task StartListenerInternal(GpsRequest request)
    {
        var appleRequest = request.ToApple();
        this.locationManager.DistanceFilter = appleRequest.DistanceFilterMeters > 0
            ? appleRequest.DistanceFilterMeters
            : CLLocationDistance.FilterNone;

#if MACOS
        // A Mac app isn't suspended in the background, so there is no background mode to turn on: Standard
        // is significant location changes and Realtime is continuous updates, the same as on iOS.
        // PausesLocationUpdatesAutomatically, ActivityType and the background indicator are iOS-only.
        if (UsesSignificantChanges(appleRequest))
            this.locationManager.StartMonitoringSignificantLocationChanges();
        else
            this.locationManager.StartUpdatingLocation();
#else
        this.locationManager.PausesLocationUpdatesAutomatically = appleRequest.PausesLocationUpdatesAutomatically;
        this.locationManager.ActivityType = appleRequest.ActivityType;

        switch (request.BackgroundMode)
        {
            case GpsBackgroundMode.Standard:
            case GpsBackgroundMode.Realtime:
                this.locationManager.AllowsBackgroundLocationUpdates = true;
                this.locationManager.ShowsBackgroundLocationIndicator = appleRequest.ShowsBackgroundLocationIndicator;
                break;
        }

        if (UsesSignificantChanges(appleRequest))
            this.locationManager.StartMonitoringSignificantLocationChanges();
        else
            this.locationManager.StartUpdatingLocation();
#endif
        this.CurrentSettings = appleRequest;
    }


    // Standard is significant location changes by definition; any mode can opt into them
    static bool UsesSignificantChanges(AppleGpsRequest request)
        => request.BackgroundMode == GpsBackgroundMode.Standard || request.UseSignificantLocationChanges;
}
