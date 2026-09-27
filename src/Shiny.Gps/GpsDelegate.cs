using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


/// <summary>
/// Base class for GPS delegates that filters incoming readings by configurable minimum and maximum distance and time thresholds before invoking <see cref="OnGpsReading"/>.
/// </summary>
/// <param name="logger">Logger used for diagnostic output.</param>
public abstract class GpsDelegate(ILogger logger) : IGpsDelegate
{
    readonly SemaphoreSlim semaphore = new(1);

    /// <summary>
    /// The logger supplied to the delegate.
    /// </summary>
    protected ILogger Logger => logger;


    /// <summary>
    /// Receives readings from the GPS manager, applies the configured filters, and forwards qualifying readings to <see cref="OnGpsReading"/>.
    /// </summary>
    /// <param name="reading">The incoming GPS reading.</param>
    public async Task OnReading(GpsReading reading)
    {
        var entered = false;
        try
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            entered = true;

            var fireReading = true;
            this.MostRecentReading = reading;

            if (this.LastReading == null)
            {
                this.Logger.LogDebug("No previous reading");
            }
            else
            {
                var dist = this.LastReading.Position.GetDistanceTo(reading.Position);
                var timeDiff = reading.Timestamp.Subtract(this.LastReading.Timestamp);

                // Maximums are OR - if either threshold is crossed, always fire
                var maxFired = false;
                if (this.MaximumDistance != null && dist >= this.MaximumDistance)
                {
                    maxFired = true;
                    this.Logger.LogDebug("Maximum distance threshold crossed: {Distance}m >= {Max}m", dist.TotalMeters, this.MaximumDistance.TotalMeters);
                }

                if (!maxFired && this.MaximumTime != null && timeDiff >= this.MaximumTime)
                {
                    maxFired = true;
                    this.Logger.LogDebug("Maximum time threshold crossed: {TimeDiff} >= {Max}", timeDiff, this.MaximumTime);
                }

                if (maxFired)
                {
                    fireReading = true;
                }
                else
                {
                    // Minimums are AND if both set, single check if only one set
                    var hasMinDist = this.MinimumDistance != null;
                    var hasMinTime = this.MinimumTime != null;

                    if (hasMinDist && hasMinTime)
                    {
                        var distMet = dist >= this.MinimumDistance;
                        var timeMet = timeDiff >= this.MinimumTime;
                        fireReading = distMet && timeMet;

                        this.Logger.DeferDistanceInfo(this.MinimumDistance!.TotalMeters, dist.TotalMeters, distMet);
                        this.Logger.DeferTimeInfo(this.MinimumTime!.Value, timeDiff, timeMet);
                    }
                    else if (hasMinDist)
                    {
                        fireReading = dist >= this.MinimumDistance;
                        this.Logger.DeferDistanceInfo(this.MinimumDistance!.TotalMeters, dist.TotalMeters, fireReading);
                    }
                    else if (hasMinTime)
                    {
                        fireReading = timeDiff >= this.MinimumTime;
                        this.Logger.DeferTimeInfo(this.MinimumTime!.Value, timeDiff, fireReading);
                    }
                }
            }

            if (fireReading)
            {
                try
                {
                    await this.OnGpsReading(reading).ConfigureAwait(false);
                }
                finally
                {
                    this.LastReading = reading;
                }
            }
        }
        finally
        {
            if (entered)
                this.semaphore.Release();
        }
    }

    
    GpsReading? lastReading;
    /// <summary>
    /// The most recent reading that passed the filters and was forwarded to <see cref="OnGpsReading"/>.
    /// </summary>
    public GpsReading? LastReading
    {
        get => this.lastReading;
        set => this.lastReading = value;
    }


    GpsReading? mostRecentReading;
    /// <summary>
    /// The most recent reading received from the GPS regardless of filtering. During <see cref="OnGpsReading"/> this is the incoming reading.
    /// </summary>
    public GpsReading? MostRecentReading
    {
        get => this.mostRecentReading;
        set => this.mostRecentReading = value;
    }


    Distance? minDistance;
    /// <summary>
    /// The minimum distance the device must move from <see cref="LastReading"/> before <see cref="OnGpsReading"/> is invoked. Null disables the filter.
    /// </summary>
    public Distance? MinimumDistance
    {
        get => this.minDistance;
        set => this.minDistance = value;
    }


    TimeSpan? minTime;
    /// <summary>
    /// The minimum time that must elapse since <see cref="LastReading"/> before <see cref="OnGpsReading"/> is invoked. Null disables the filter.
    /// </summary>
    public TimeSpan? MinimumTime
    {
        get => this.minTime;
        set => this.minTime = value;
    }


    Distance? maxDistance;
    /// <summary>
    /// If set, <see cref="OnGpsReading"/> is always invoked once the device has moved at least this distance since <see cref="LastReading"/>, bypassing the minimum filters.
    /// </summary>
    public Distance? MaximumDistance
    {
        get => this.maxDistance;
        set => this.maxDistance = value;
    }


    TimeSpan? maxTime;
    /// <summary>
    /// If set, <see cref="OnGpsReading"/> is always invoked once this much time has elapsed since <see cref="LastReading"/>, bypassing the minimum filters.
    /// </summary>
    public TimeSpan? MaximumTime
    {
        get => this.maxTime;
        set => this.maxTime = value;
    }


    /// <summary>
    /// Invoked for each GPS reading that passes the configured filters.
    /// </summary>
    /// <param name="reading">The filtered GPS reading.</param>
    protected abstract Task OnGpsReading(GpsReading reading);
}