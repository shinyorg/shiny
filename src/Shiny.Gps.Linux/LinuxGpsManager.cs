using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Locations.GeoClue;
using Tmds.DBus.Protocol;

namespace Shiny.Locations;


/// <summary>
/// <see cref="IGpsManager"/> on GeoClue2, the freedesktop location service on the D-Bus system bus. GeoClue
/// works out where the machine is from Wi-Fi, the IP address, a GPS receiver or a modem, at the accuracy
/// level asked for (<see cref="LinuxGpsOptions"/>).
/// </summary>
/// <remarks>
/// <para>GeoClue has no "ask" of its own: its agent (GNOME's location privacy setting, the location portal)
/// decides when a client starts. So <see cref="RequestAccess"/> starts a client and sees whether it's
/// allowed, and <see cref="GetCurrentStatus"/> reports what has been learned that way - <see cref="AccessState.Unknown"/>
/// until then, <see cref="AccessState.NotSupported"/> when GeoClue isn't on the system at all.</para>
/// <para>Background modes mean nothing here: a desktop process isn't suspended, so <see cref="GpsBackgroundMode.Standard"/>
/// and <see cref="GpsBackgroundMode.Realtime"/> both listen continuously, like <see cref="GpsBackgroundMode.None"/>.
/// A background listener is still persisted and restarted at startup when <see cref="GpsRequest.AutoRestart"/> is set,
/// as on the other platforms.</para>
/// <para><see cref="GpsReadingReceived"/> and the <see cref="IGpsDelegate"/>s are called on the D-Bus thread.</para>
/// </remarks>
public class LinuxGpsManager : IGpsManager, IShinyStartupTask, IAsyncDisposable
{
    const string CurrentSettingsKey = "Shiny.Locations.GpsManager.CurrentSettings";

    readonly IServiceProvider services;
    readonly IKeyValueStore store;
    readonly LinuxGpsOptions options;
    readonly ILogger logger;
    readonly GeoClueClient geoClue = new();
    readonly StationaryDetector stationaryDetector = new();
    readonly SemaphoreSlim gate = new(1, 1);
    readonly Task probe;

    AccessState status = AccessState.Unknown;
    IDisposable? watch;
    string? activeClient;
    GpsReading? lastReading;
    TaskCompletionSource<GpsReading>? nextReading;


    /// <summary>Created by the container; see <c>AddLinuxGps</c>.</summary>
    public LinuxGpsManager(
        IServiceProvider services,
        [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
        LinuxGpsOptions options,
        ILogger<IGpsManager> logger
    )
    {
        this.services = services;
        this.store = store;
        this.options = options;
        this.logger = logger;
        this.currentListener = store.Get<GpsRequest>(CurrentSettingsKey);
        this.probe = this.Probe();
    }


    /// <inheritdoc />
    public event EventHandler<GpsReading>? GpsReadingReceived;


    GpsRequest? currentListener;
    /// <inheritdoc />
    public GpsRequest? CurrentListener
    {
        get => this.currentListener;
        private set
        {
            this.currentListener = value;
            var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
            this.store.SetOrRemove(CurrentSettingsKey, bg != GpsBackgroundMode.None ? value : null);
        }
    }


    /// <summary>What has been learned so far - GeoClue can't be asked without starting a client.</summary>
    public AccessState GetCurrentStatus(GpsRequest request) => this.status;


    /// <inheritdoc />
    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        await this.probe.ConfigureAwait(false);
        if (this.status == AccessState.NotSupported)
            return this.status;

        await this.gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // a running listener is already the answer
            if (this.activeClient != null)
                return this.status;

            var client = await this.Configure(request).ConfigureAwait(false);
            await this.geoClue.Start(client).ConfigureAwait(false);
            this.status = AccessState.Available;

            try
            {
                await this.geoClue.Stop(client).ConfigureAwait(false);
            }
            catch (DBusExceptionBase ex)
            {
                this.logger.LogDebug(ex, "Could not stop the GeoClue client after the access check");
            }
        }
        catch (DBusErrorReplyException ex)
        {
            this.Learn(ex);
        }
        catch (Exception ex) when (ex is DBusExceptionBase or InvalidOperationException)
        {
            this.logger.LogWarning(ex, "GeoClue could not be reached");
        }
        finally
        {
            this.gate.Release();
        }
        return this.status;
    }


    /// <summary>
    /// The last reading. With a <paramref name="timeout"/> and no reading yet, waits up to that long for the
    /// first one - but only while a listener is running, since nothing else produces one.
    /// </summary>
    public async Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        if (this.lastReading != null || timeout == null)
            return this.lastReading;

        // no reading yet: worth waiting for one only while something is listening
        var waiter = this.nextReading;
        if (this.activeClient == null || waiter == null)
            return null;

        try
        {
            return await waiter.Task.WaitAsync(timeout.Value).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }


    /// <inheritdoc />
    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new InvalidOperationException("There is already an active GPS listener");

        await this.StartCore(request).ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task StopListener()
    {
        await this.gate.WaitAsync().ConfigureAwait(false);
        try
        {
            this.watch?.Dispose();
            this.watch = null;

            if (this.activeClient != null)
            {
                try
                {
                    await this.geoClue.Stop(this.activeClient).ConfigureAwait(false);
                }
                catch (DBusExceptionBase ex)
                {
                    this.logger.LogDebug(ex, "Could not stop the GeoClue client");
                }
                this.activeClient = null;
            }

            this.nextReading?.TrySetCanceled();
            this.nextReading = null;
            this.stationaryDetector.Reset();
            this.CurrentListener = null;
        }
        finally
        {
            this.gate.Release();
        }
    }


    /// <summary>Restarts a persisted background listener with <see cref="GpsRequest.AutoRestart"/> set.</summary>
    public async void Start()
    {
        if (this.CurrentListener is not { AutoRestart: true } request)
        {
            this.CurrentListener = null;
            return;
        }

        try
        {
            await this.StartCore(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.CurrentListener = null;
            this.logger.LogInformation(ex, "Failed to restart the GPS listener");
        }
    }


    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        this.watch?.Dispose();
        this.watch = null;
        await this.geoClue.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }


    async Task StartCore(GpsRequest request)
    {
        await this.probe.ConfigureAwait(false);
        if (this.status == AccessState.NotSupported)
            throw new InvalidOperationException("GeoClue is not available on this system");

        await this.gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // checked again under the gate - two StartListener calls racing past the CurrentListener check would
            // otherwise both watch and start the client
            if (this.activeClient != null)
                throw new InvalidOperationException("There is already an active GPS listener");

            var client = await this.Configure(request).ConfigureAwait(false);
            this.nextReading = new TaskCompletionSource<GpsReading>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.watch = await this.geoClue.WatchLocationUpdated(client, this.OnLocationUpdated).ConfigureAwait(false);

            try
            {
                await this.geoClue.Start(client).ConfigureAwait(false);
            }
            catch
            {
                this.watch.Dispose();
                this.watch = null;
                this.nextReading = null;
                throw;
            }

            this.activeClient = client;
            this.status = AccessState.Available;
            this.CurrentListener = request;
        }
        catch (DBusErrorReplyException ex)
        {
            this.Learn(ex);
            throw;
        }
        finally
        {
            this.gate.Release();
        }
    }


    Task<string> Configure(GpsRequest request) => this.geoClue.GetClient(
        this.options.DesktopId,
        GeoClueMapping.AccuracyFor(request, this.options),
        this.options.DistanceThresholdMeters,
        this.options.TimeThresholdSeconds
    );


    async void OnLocationUpdated(string locationPath)
    {
        try
        {
            var reading = await this.geoClue.ReadLocation(locationPath).ConfigureAwait(false);
            if (this.stationaryDetector.Detect(reading, this.options.StationaryMetersThreshold, this.options.StationarySecondsThreshold))
                reading = reading with { IsStationary = true };

            this.lastReading = reading;
            this.nextReading?.TrySetResult(reading);
            this.GpsReadingReceived?.Invoke(this, reading);

            await this.services
                .RunDelegates<IGpsDelegate>(x => x.OnReading(reading), this.logger)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing a GeoClue location");
        }
    }


    async Task Probe()
    {
        try
        {
            if (!await this.geoClue.IsServiceAvailable().ConfigureAwait(false))
                this.status = AccessState.NotSupported;
        }
        catch (Exception ex)
        {
            // no system bus at all (a container, a sandbox without it): nothing can locate
            this.logger.LogDebug(ex, "No D-Bus system bus - GeoClue is not available");
            this.status = AccessState.NotSupported;
        }
    }


    void Learn(DBusErrorReplyException ex)
    {
        if (GeoClueMapping.AccessFromError(ex.ErrorName) is { } learned)
            this.status = learned;

        this.logger.LogWarning("GeoClue refused the client: {Error} {Message}", ex.ErrorName, ex.ErrorMessage);
    }
}
