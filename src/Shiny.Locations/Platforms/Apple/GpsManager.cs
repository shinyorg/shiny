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
        if (this.CurrentSettings != null)
        {
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

    public AccessState CurrentStatus => throw new NotImplementedException();

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
        if (this.CurrentSettings == null)
            return Task.CompletedTask;

        this.locationManager.AllowsBackgroundLocationUpdates = false;

        if (this.CurrentSettings.UseSignificantLocationChanges)
            this.locationManager.StopMonitoringSignificantLocationChanges();
        else
            this.locationManager.StopUpdatingLocation();

        this.CurrentSettings = null;
        return Task.CompletedTask;
    }


    protected virtual async Task StartListenerInternal(GpsRequest request)
    {
        (await this.RequestAccess(request).ConfigureAwait(false)).Assert();

        this.locationManager.DistanceFilter = request.DistanceFilterMeters;
        this.locationManager.DesiredAccuracy = request.Accuracy switch
        {
            //CLLocation.AccurracyBestForNavigation;
            //CLLocation.AccuracyReduced
            GpsAccuracy.Highest => CLLocation.AccuracyBest,
            GpsAccuracy.High => CLLocation.AccuracyNearestTenMeters,
            GpsAccuracy.Normal => CLLocation.AccuracyHundredMeters,
            GpsAccuracy.Low => CLLocation.AccuracyKilometer,
            GpsAccuracy.Lowest => CLLocation.AccuracyThreeKilometers
        };

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
        if (useSignificant)
            this.locationManager.StartMonitoringSignificantLocationChanges();
        else
            this.locationManager.StartUpdatingLocation();
    }
}
