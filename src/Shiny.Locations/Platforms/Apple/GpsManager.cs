using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;

namespace Shiny.Locations;


[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
public class GpsManager(
    IServiceProvider services,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<IGpsManager> logger
) : IGpsManager, IShinyStartupTask
{
    const string CurrentSettingsKey = "Shiny.Locations.GpsManager.CurrentSettings";

    /// <summary>
    /// How long <see cref="RequestAccess"/> waits for the session to say anything at all.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>This covers the callback arriving, never the user reading the prompt.</b> The diagnostic
    /// handler fires as soon as the session is created — first to say a request is in progress, then again
    /// with the answer — so the only thing worth a wall clock is the first of those never coming. Arming it
    /// across the second would throw a <see cref="TimeoutException"/> out from under somebody still looking
    /// at an "allow always" dialog, which is the Android bug in issue #1625 wearing a different platform.
    /// The timer is disarmed the moment the OS says it is asking.
    /// </remarks>
    static readonly TimeSpan NoDiagnosticTimeout = TimeSpan.FromSeconds(30);

    CLBackgroundActivitySession? bgSession;
    CLLocationUpdater? updater;

    /// <summary>The live authorization session, and the requirement it was created under.</summary>
    /// <remarks>
    /// ⚠️ <b>A session is bound to the requirement it was created with, so the pair moves together.</b> One
    /// opened for <see cref="CLServiceSessionAuthorizationRequirement.WhenInUse"/> can never produce an
    /// "always" grant — a background request has to replace it. This used to be a bare
    /// <c>session ??= CreateSession(...)</c>, which meant the first caller decided the requirement for the
    /// life of the process: an app that asked for foreground location first (any app with a live map) could
    /// never afterwards raise the "always" prompt at all.
    /// </remarks>
    CLServiceSession? session;

    /// <inheritdoc cref="session"/>
    CLServiceSessionAuthorizationRequirement? sessionRequirement;

    /// <summary>
    /// Serializes <see cref="RequestAccess"/>. Each call replaces the session and waits on a completion
    /// source the new session's handler owns, so two in flight together would leave the first waiting on a
    /// handler that no longer exists.
    /// </summary>
    readonly SemaphoreSlim authGate = new(1, 1);


    AppleGpsRequest? currentSettings = store.Get<AppleGpsRequest>(CurrentSettingsKey);
    public AppleGpsRequest? CurrentSettings
    {
        get => this.currentSettings;
        set
        {
            this.currentSettings = value;
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            store.SetOrRemove(CurrentSettingsKey, bg != GpsBackgroundMode.None ? value : null);
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
        await this.authGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await this.RequestAccessCore(request).ConfigureAwait(false);
        }
        finally
        {
            this.authGate.Release();
        }
    }


    async Task<AccessState> RequestAccessCore(GpsRequest request)
    {
        var requirement = request.BackgroundMode == GpsBackgroundMode.None
            ? CLServiceSessionAuthorizationRequirement.WhenInUse
            : CLServiceSessionAuthorizationRequirement.Always;

        var access = this.GetCurrentStatus(request);

        // A prompt cannot revisit either of these - the user has decided, or the device has - and opening a
        // session to be told so costs a round trip and a dialog nobody sees.
        if (access is AccessState.Denied or AccessState.Disabled)
            return access;

        // Already granted with a session alive to hold it. Anything else falls through and asks, including
        // AccessState.Restricted: "authorized when in use" reports as restricted the moment a background
        // request asks about it, and that is exactly the state an "always" session exists to upgrade.
        // Reduced accuracy lands on the same value and is worth re-asking too, since the session carries the
        // full-accuracy purpose key.
        if (access == AccessState.Available && this.CoveredBySession(requirement))
            return access;

        var tcs = new TaskCompletionSource<AccessState>();
        using var cts = new CancellationTokenSource(NoDiagnosticTimeout);
        using var reg = cts.Token.Register(
            () => tcs.TrySetException(new TimeoutException("The GPS authorization session reported no status"))
        );

        var fullAccuracy = request.RequestPreciseAccuracy
            ? "shinygps"
            : String.Empty;

        var replaced = this.session;
        this.session = CLServiceSession.CreateSession(
            requirement,
            fullAccuracy,
            DispatchQueue.MainQueue,
            diag =>
            {
                // ⚠️ The session outlives the request that opened it, and the OS re-runs this handler every
                // time authorization changes - including from the Settings app, minutes later, long after
                // the task below was awaited. Anything that escapes here escapes into a native callback,
                // which takes the process with it, so the whole body is guarded.
                try
                {
                    // Whoever answered first owns the result; a later change re-runs the handler but has
                    // nobody waiting on it, and the timer it would touch has already been disposed.
                    if (tcs.Task.IsCompleted)
                        return;

                    if (diag.AuthorizationRequestInProgress)
                    {
                        // The OS is showing the dialog: stop the clock and wait for the handler to come back
                        // with what the user chose. See NoDiagnosticTimeout.
                        cts.CancelAfter(Timeout.InfiniteTimeSpan);
                        return;
                    }

                    var currentAccess = AccessState.Unknown;
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
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "The GPS authorization session handler failed");
                    tcs.TrySetException(ex);
                }
            }
        );
        this.sessionRequirement = requirement;

        // The one it replaces goes only once its replacement is alive. Dropping to no session at all, however
        // briefly, is how an app tells CoreLocation it is finished with location.
        replaced?.Invalidate();

        return await tcs.Task.ConfigureAwait(false);
    }


    /// <summary>
    /// Whether the session already open satisfies <paramref name="requirement"/>. An "always" session covers
    /// a when-in-use request, so a foreground caller never tears down a background grant to ask for less.
    /// </summary>
    bool CoveredBySession(CLServiceSessionAuthorizationRequirement requirement)
        => this.session != null && (
            this.sessionRequirement == requirement ||
            this.sessionRequirement == CLServiceSessionAuthorizationRequirement.Always
        );


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
