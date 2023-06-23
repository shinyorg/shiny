using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public abstract class DeferredGpsDelegate : NotifyPropertyChanged, IGpsDelegate
{
    readonly ILogger logger;
    public DeferredGpsDelegate(ILogger logger) => this.logger = logger;


    public async Task OnReading(GpsReading reading)
    {
        var fireReading = false;

        if (this.LastReading == null)
        {
            fireReading = true;
            this.logger.LogDebug("No previous reading");
        }
        else
        {
            if (this.MinimumDistance != null)
            {
                var dist = this.LastReading.Position.GetDistanceTo(reading.Position);
                fireReading = dist >= this.MinimumDistance;

                this.logger.DeferDistanceInfo(this.MinimumDistance!.TotalMeters, dist.TotalMeters, fireReading);
            }

            if (!fireReading && this.MinimumTime != null)
            {
                var timeDiff = reading.Timestamp.Subtract(this.LastReading.Timestamp);
                fireReading = timeDiff >= this.MinimumTime;

                this.logger.DeferTimeInfo(this.MinimumTime!.Value, timeDiff, fireReading);
            }
        }

        if (fireReading)
        {
            try
            {
                await this.OnDeferredReading(reading).ConfigureAwait(false);
            }
            finally
            {
                this.LastReading = reading;
            } 
        }
    }


    GpsReading? lastReading;
    public GpsReading? LastReading
    {
        get => this.lastReading;
        set => this.Set(ref this.lastReading, value);
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


    protected abstract Task OnDeferredReading(GpsReading reading);
}