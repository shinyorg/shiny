using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public abstract class GpsDelegate(ILogger logger) : IGpsDelegate
{
    readonly SemaphoreSlim semaphore = new(1);
    protected ILogger Logger => logger;


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
    /// This is the last GPS reading before OnReading is raised
    /// </summary>
    public GpsReading? LastReading
    {
        get => this.lastReading;
        set => this.lastReading = value;
    }


    GpsReading? mostRecentReading;
    /// <summary>
    /// This is the most recent reading from the GPS outside of the filters
    /// If you are gettings this as of OnReading, it will be the current incoming reading
    /// </summary>
    public GpsReading? MostRecentReading
    {
        get => this.mostRecentReading;
        set => this.mostRecentReading = value;
    }
    
    
    Distance? minDistance;
    public Distance? MinimumDistance
    {
        get => this.minDistance;
        set => this.minDistance = value;
    }


    TimeSpan? minTime;
    public TimeSpan? MinimumTime
    {
        get => this.minTime;
        set => this.minTime = value;
    }


    Distance? maxDistance;
    public Distance? MaximumDistance
    {
        get => this.maxDistance;
        set => this.maxDistance = value;
    }


    TimeSpan? maxTime;
    public TimeSpan? MaximumTime
    {
        get => this.maxTime;
        set => this.maxTime = value;
    }


    protected abstract Task OnGpsReading(GpsReading reading);
}