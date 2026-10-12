using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;

namespace Shiny.Locations;


/// <summary>
/// Default <see cref="IRegionGeofenceManager"/> - drives the GPS listener and restores monitoring after
/// the app is relaunched.
/// </summary>
public class RegionGeofenceManager : IRegionGeofenceManager, IShinyStartupTask
{
    const string StartedKey = "regiongeofence.started";

    readonly IGpsManager gpsManager;
    readonly IKeyValueStore state;
    readonly RegionEvaluator evaluator;
    readonly RegionGeofenceConfig config;
    readonly ILogger logger;


    /// <summary>Creates the manager. Registered as a singleton by <c>AddRegionGeofencing</c>.</summary>
    public RegionGeofenceManager(
        IGpsManager gpsManager,
        [FromKeyedServices(StoreKeys.Default)] IKeyValueStore state,
        RegionEvaluator evaluator,
        RegionGeofenceConfig config,
        ILogger<RegionGeofenceManager> logger
    )
    {
        this.gpsManager = gpsManager;
        this.state = state;
        this.evaluator = evaluator;
        this.config = config;
        this.logger = logger;
    }


    /// <inheritdoc />
    public bool IsStarted => this.state.Get<bool>(StartedKey);


    /// <inheritdoc />
    public Task<AccessState> RequestAccess()
        => this.gpsManager.RequestAccess(this.config.GpsRequest);


    /// <inheritdoc />
    public async Task Start()
    {
        if (this.IsStarted && this.gpsManager.CurrentListener != null)
            return;

        await this.gpsManager.StartListener(this.config.GpsRequest).ConfigureAwait(false);
        this.state.Set(StartedKey, true);
        this.logger.LogInformation("Region geofencing started against {Count} region set(s)", this.config.RegionSets.Count);
    }


    /// <inheritdoc />
    public async Task Stop()
    {
        await this.gpsManager.StopListener().ConfigureAwait(false);
        this.state.Remove(StartedKey);
        this.evaluator.Reset();
        this.logger.LogInformation("Region geofencing stopped");
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<CurrentRegion>> GetCurrent(CancellationToken cancellationToken = default)
    {
        var reading = await this.gpsManager.GetLastReading(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        if (reading == null)
            return Array.Empty<CurrentRegion>();

        return await this.evaluator.Current(reading.Position, cancellationToken).ConfigureAwait(false);
    }


    // Restores the listener after the OS relaunches the app - the persisted region ids are still there, so
    // monitoring picks up where it left off instead of replaying entries.
    async void IShinyStartupTask.Start()
    {
        try
        {
            if (this.IsStarted && this.gpsManager.CurrentListener == null)
                await this.gpsManager.StartListener(this.config.GpsRequest).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to restore region geofence monitoring");
        }
    }
}
