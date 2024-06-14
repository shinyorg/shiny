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
        try
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);

            this.MostRecentReading = reading;
            
            var fireReading = false;
            if (this.LastReading == null)
            {
                fireReading = true;
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
        set => this.Set(ref this.lastReading, value);
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