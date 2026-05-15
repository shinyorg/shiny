using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
public class GpsManager(
    IServiceProvider services,
    ILogger<IGpsManager> logger
) : NotifyPropertyChanged, IGpsManager, IShinyStartupTask
{
    CLBackgroundActivitySession? bgSession;
    CLServiceSession? session;
    CLLocationUpdater? updater;


    public AppleGpsRequest? CurrentSettings
    {
        get;
        set
        {
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            if (bg != GpsBackgroundMode.None)
            {
                this.Set(ref field, value);
            }
            else
            {
                // don't remember across sessions
                this.Set(ref field, null);
                field = value;
            }
        }
    }


    public GpsRequest? CurrentListener => this.CurrentSettings;

    // could check against current listener
    // AccessState currentAccess = AccessState.Unknown; // TODO: this won't apply for different request types unless I record deltas of the request

    public AccessState GetCurrentStatus(GpsRequest request)
    {
        using var locationManager = new CLLocationManager();
        return locationManager.GetCurrentStatus(
            request.BackgroundMode != GpsBackgroundMode.None,
            request.RequestPreciseAccuracy
        );
    }


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var access = this.GetCurrentStatus(request);
        if (this.session != null && access != AccessState.Unknown)
            return access;

        var requirement = request.BackgroundMode == GpsBackgroundMode.None
            ? CLServiceSessionAuthorizationRequirement.WhenInUse
            : CLServiceSessionAuthorizationRequirement.Always;

        var tcs = new TaskCompletionSource<AccessState>();
        var fullAccuracy = request.RequestPreciseAccuracy
            ? "shinygps"
            : String.Empty;

        this.session ??= CLServiceSession.CreateSession(
            requirement,
            fullAccuracy,
            DispatchQueue.MainQueue,
            diag =>
            {
                var currentAccess = AccessState.Unknown;
                if (diag.AuthorizationRequestInProgress)
                    return;

                if (request.BackgroundMode != GpsBackgroundMode.None)
                {
                    if (!diag.AlwaysAuthorizationDenied)
                    {
                        currentAccess = AccessState.Available;
                        if (request.RequestPreciseAccuracy && diag.FullAccuracyDenied)
                            currentAccess = AccessState.Restricted;
                    }
                    else if (!diag.AuthorizationRestricted)
                        currentAccess = AccessState.Restricted;

                    else
                        currentAccess = AccessState.Denied;
                }
                else
                {
                    currentAccess = diag.AuthorizationDenied
                        ? AccessState.Denied
                        : AccessState.Available;
                }

                tcs.TrySetResult(currentAccess);
            }
        );

        return await tcs.Task.ConfigureAwait(false);
    }


    GpsReading? lastReading;
    public Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        if (this.lastReading != null)
            return Task.FromResult<GpsReading?>(this.lastReading);

        using var locationManager = new CLLocationManager();
        if (locationManager.Location != null)
            return Task.FromResult<GpsReading?>(locationManager.Location.FromNative());

        return Task.FromResult<GpsReading?>(null);
    }


    public event EventHandler<GpsReading>? GpsReadingReceived;


    public async Task StartListener(GpsRequest request)
    {
        if (this.updater != null)
            throw new InvalidOperationException("Already GPS listener running");

        if (request.BackgroundMode != GpsBackgroundMode.None)
            this.bgSession = CLBackgroundActivitySession.Create();

        var appleRequest = request.ToApple();
        var modernActivityType = appleRequest.ActivityType switch
        {
            CLActivityType.Airborne => CLLiveUpdateConfiguration.Airborne,
            CLActivityType.Fitness => CLLiveUpdateConfiguration.Fitness,
            CLActivityType.AutomotiveNavigation => CLLiveUpdateConfiguration.AutomotiveNavigation,
            CLActivityType.OtherNavigation => CLLiveUpdateConfiguration.OtherNavigation,
            _ => CLLiveUpdateConfiguration.Default
        };

        //https://developer.apple.com/videos/play/wwdc2023/10180/
        //https://developer.apple.com/documentation/corelocation/supporting-live-updates-in-swiftui-and-mac-catalyst-apps
        this.updater = CLLocationUpdater.CreateLiveUpdates(
            modernActivityType,
            new DispatchQueue("shinygps"),
            async update =>
            {
                if (update.Location == null || this.updater == null)
                    return;

                var epochTimestamp = Convert.ToInt64(update.Location.Timestamp.SecondsSince1970);
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(epochTimestamp);

                // update.Location.Floor.Level
                var reading = new GpsReading(
                    update.Location.Coordinate.FromNative(),
                    update.Location.HorizontalAccuracy,
                    timestamp,
                    update.Location.Course,
                    update.Location.VerticalAccuracy,
                    update.Location.Altitude,
                    update.Location.Speed,
                    update.Location.SpeedAccuracy,
                    update.Location.Floor?.Level.ToInt32() ?? 0,
                    update.Stationary
                );
                this.lastReading = reading;
                this.GpsReadingReceived?.Invoke(this, reading);

                await services
                    .RunDelegates<IGpsDelegate>(
                        x => x.OnReading(reading),
                        logger
                    )
                    .ConfigureAwait(false);
            }
        );
        this.updater!.Resume();
        this.CurrentSettings = appleRequest;
    }


    public Task StopListener()
    {
        this.updater?.Invalidate();
        this.updater = null;

        this.bgSession?.Invalidate();
        this.bgSession = null;

        this.CurrentSettings = null;

        return Task.CompletedTask;
    }


    public async void Start()
    {
        if (this.CurrentListener is not { AutoRestart: true })
        {
            this.CurrentSettings = null;
            return;
        }

        try
        {
            await this.StartListener(this.CurrentListener!);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Failed to restart GPS listener");
        }
    }
}
