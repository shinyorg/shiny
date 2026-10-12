using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


/// <summary>
/// The GPS listener behind region geofencing. Registered as an <see cref="IGpsDelegate"/> by
/// <c>AddRegionGeofencing</c>; every reading that survives the configured distance/time filters is
/// evaluated against the monitored region sets.
/// </summary>
public class RegionGeofenceGpsDelegate : GpsDelegate
{
    readonly RegionEvaluator evaluator;


    /// <summary>Creates the delegate and applies the reading filters from <paramref name="config"/>.</summary>
    public RegionGeofenceGpsDelegate(
        RegionEvaluator evaluator,
        RegionGeofenceConfig config,
        ILogger<RegionGeofenceGpsDelegate> logger
    ) : base(logger)
    {
        this.evaluator = evaluator;

        this.MinimumDistance = config.MinimumDistance;
        this.MinimumTime = config.MinimumTime;
        this.MaximumDistance = config.MaximumDistance;
        this.MaximumTime = config.MaximumTime;
    }


    /// <inheritdoc />
    protected override async Task OnGpsReading(GpsReading reading)
    {
        var changes = await this.evaluator.Evaluate(reading.Position).ConfigureAwait(false);

        if (changes.Count > 0)
            this.Logger.LogDebug("Geofence reading produced {Count} transition(s)", changes.Count);
    }
}
