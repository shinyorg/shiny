using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public abstract class GpsDelegate(ILogger logger) : NotifyPropertyChanged, IGpsDelegate
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
            if (this.DetectStationary)
                this.DetectIfStationary(reading);

            var fireReading = true;
            this.MostRecentReading = reading;

            if (this.LastReading == null)
            {
                this.Logger.LogDebug("No previous reading");
            }
            else
            {
                if (this.MinimumDistance != null)
                {
                    var dist = this.LastReading.Position.GetDistanceTo(reading.Position);
                    fireReading = dist >= this.MinimumDistance;

                    this.Logger.DeferDistanceInfo(this.MinimumDistance!.TotalMeters, dist.TotalMeters, fireReading);
                }

                if (!fireReading && this.MinimumTime != null)
                {
                    var timeDiff = reading.Timestamp.Subtract(this.LastReading.Timestamp);
                    fireReading = timeDiff >= this.MinimumTime;

                    this.Logger.DeferTimeInfo(this.MinimumTime!.Value, timeDiff, fireReading);
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

    
    DateTimeOffset? lastMovement;
    
    internal void DetectIfStationary(GpsReading reading)
    {
        this.lastMovement ??= reading.Timestamp;

        if (this.MostRecentReading != null)
        {
            var distance = reading.Position.GetDistanceTo(this.lastReading.Position);
            if (distance.TotalMeters < StationaryMetersThreshold)
            {
                var time = reading.Timestamp - this.lastMovement;
                if (time.Value.TotalSeconds >= StationarySecondsThreshold)
                {
                    if (this.IsStationary)
                    {
                        logger.LogDebug("Still stationary");
                    }
                    else
                    {
                        this.IsStationary = true;
                        logger.LogDebug("Stationary Detected");
                    }
                }
                else
                {
                    logger.LogDebug("Stationary Detected, but insufficient time has past: " + time);
                }
            }
            else
            {
                logger.LogDebug("Stationary Threshold Not Reached - {Meters}m", distance.TotalMeters);
                this.lastMovement = reading.Timestamp;
                if (this.IsStationary)
                {
                    this.IsStationary = false;
                    logger.LogDebug("Stationary Changed to In-Motion");
                }
                else
                {
                    logger.LogDebug("Still in-motion");
                }
            }
        }
    }
    
    
    protected int StationaryMetersThreshold { get; set; } = 10;
    protected int StationarySecondsThreshold { get; set; } = 30;
    protected bool DetectStationary { get; set; }
    

    GpsReading? lastReading;
    /// <summary>
    /// This is the last GPS reading before OnReading is raised
    /// </summary>
    public GpsReading? LastReading
    {
        get => this.lastReading;
        set => this.Set(ref this.lastReading, value);
    }


    bool isStationary;
    /// <summary>
    /// Will look at all 
    /// </summary>
    public bool IsStationary
    {
        get => this.isStationary;
        private set => this.Set(ref this.isStationary, value);
    }
    

    GpsReading? mostRecentReading;
    /// <summary>
    /// This is the most recent reading from the GPS outside of the filters
    /// If you are gettings this as of OnReading, it will be the current incoming reading
    /// </summary>
    public GpsReading? MostRecentReading
    {
        get => this.mostRecentReading;
        set => this.Set(ref this.mostRecentReading, value);
    }
    
    
    Distance? minDistance;
    public Distance? MinimumDistance
    {
        get => this.minDistance;
        set => this.Set(ref this.minDistance, value);
    }


    TimeSpan? minTime;
    public TimeSpan? MinimumTime
    {
        get => this.minTime;
        set => this.Set(ref this.minTime, value);
    }


    protected abstract Task OnGpsReading(GpsReading reading);
}