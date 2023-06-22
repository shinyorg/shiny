using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public abstract class DeferredGpsDelegate : NotifyPropertyChanged, IGpsDelegate
{
    readonly DeferredGpsConfiguration configuration;
    readonly ILogger logger;


    public DeferredGpsDelegate(IServiceProvider services)
    {
        this.configuration = services.GetRequiredService<DeferredGpsConfiguration>();
        this.logger = services.GetRequiredService<ILogger<DeferredGpsDelegate>>();
    }


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
            if (this.configuration.MinDistance != null)
            {
                var dist = this.LastReading.Position.GetDistanceTo(reading.Position);
                fireReading = dist >= this.configuration.MinDistance;

                this.logger.DeferDistanceInfo(this.configuration.MinDistance!.TotalMeters, dist.TotalMeters, fireReading);
            }

            if (!fireReading && this.configuration.MinTime != null)
            {
                var timeDiff = reading.Timestamp.Subtract(this.LastReading.Timestamp);
                fireReading = timeDiff >= this.configuration.MinTime;

                this.logger.DeferTimeInfo(this.configuration.MinTime!.Value, timeDiff, fireReading);
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


    protected abstract Task OnDeferredReading(GpsReading reading);
}

// maybe make this mutable so it can be changed in app
public record DeferredGpsConfiguration(
    Distance? MinDistance,
    TimeSpan? MinTime
);