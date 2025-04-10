using System;
using System.Reactive.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
public class GpsManager(IServiceProvider services, ILogger<IGpsManager> logger) : IGpsManager, IShinyStartupTask
{
    CLBackgroundActivitySession? bgSession;
    CLServiceSession? session;
    CLLocationUpdater? updater;
    
    
    public GpsRequest? CurrentListener { get; set; }

    // could check against current listener
    AccessState currentAccess = AccessState.Unknown; // TODO: this won't apply for different request types unless I record deltas of the request
    public AccessState GetCurrentStatus(GpsRequest request) => this.currentAccess;


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        if (this.session != null && this.currentAccess != AccessState.Unknown)
            return this.currentAccess;
        
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
                if (diag.AuthorizationRequestInProgress)
                    return;
                
                if (request.BackgroundMode != GpsBackgroundMode.None)
                {
                    if (!diag.AlwaysAuthorizationDenied)
                    {
                        this.currentAccess = AccessState.Available;
                        if (request.RequestPreciseAccuracy && diag.FullAccuracyDenied)
                            this.currentAccess = AccessState.Restricted;
                    }
                    else if (!diag.AuthorizationRestricted)
                        this.currentAccess = AccessState.Restricted;
                    
                    else
                        this.currentAccess = AccessState.Denied;
                }
                else
                {
                    this.currentAccess = diag.AuthorizationDenied 
                        ? AccessState.Denied 
                        : AccessState.Available;
                }

                tcs.SetResult(this.currentAccess);
            }
        );

        return await tcs.Task.ConfigureAwait(false);    
    }


    // TODO: I'll have to store this
    public IObservable<GpsReading?> GetLastReading() => Observable.Empty<GpsReading?>();

    
    readonly ShinySubject<GpsReading> readSubject = new();
    public IObservable<GpsReading> WhenReading() => this.readSubject;


    public async Task StartListener(GpsRequest request)
    {
        if (this.updater != null)
            throw new InvalidOperationException("Already GPS listener running");

        (await this.RequestAccess(request)).Assert();
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
        
        //https://developer.apple.com/documentation/corelocation/supporting-live-updates-in-swiftui-and-mac-catalyst-apps
        this.updater = CLLocationUpdater.CreateLiveUpdates(
            modernActivityType,
            new DispatchQueue("shinygps"), 
            async update =>
            {
                if (update.Location == null)
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
                    update.Location.SpeedAccuracy
                );
                this.readSubject.OnNext(reading);
                
                await services
                    .RunDelegates<IGpsDelegate>(
                        x => x.OnReading(reading),
                        logger
                    )
                    .ConfigureAwait(false);
            }
        );
        this.CurrentListener = appleRequest;
    }

    
    public Task StopListener()
    {
        this.CurrentListener = null;
        
        this.updater?.Invalidate();
        this.updater = null;
        
        this.session?.Invalidate();
        this.session = null;
        
        this.bgSession?.Invalidate();
        this.bgSession = null;
        
        return Task.CompletedTask;
    }
    

    public async void Start()
    {
        if (this.CurrentListener == null)
            return;
        
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