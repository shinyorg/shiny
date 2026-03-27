using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace Sample.Maui.Delegates;

public class SampleGpsDelegate(ILogger<SampleGpsDelegate> logger) : IGpsDelegate
{
    public Task OnReading(GpsReading reading)
    {
        logger.LogInformation("GPS Reading: Lat={Lat}, Lng={Lng}, Alt={Alt}",
            reading.Position.Latitude, reading.Position.Longitude, reading.Altitude);
        return Task.CompletedTask;
    }
}
