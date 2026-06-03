using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;

namespace Sample.Shared.Maui.Delegates;


public class SampleGpsDelegate(ILogger<SampleGpsDelegate> logger, IEventStore events) : GpsDelegate(logger)
{
    protected override Task OnGpsReading(GpsReading reading)
    {
        this.Logger.LogInformation("GPS Reading: Lat={Lat}, Lng={Lng}, Alt={Alt}",
            reading.Position.Latitude, reading.Position.Longitude, reading.Altitude);

        return events.Add(
            "GPS",
            $"{reading.Position.Latitude:F5}, {reading.Position.Longitude:F5}",
            new Dictionary<string, string?>
            {
                ["Latitude"] = reading.Position.Latitude.ToString("F6"),
                ["Longitude"] = reading.Position.Longitude.ToString("F6"),
                ["Altitude"] = reading.Altitude.ToString("F2"),
                ["Speed"] = reading.Speed.ToString("F2"),
                ["Heading"] = reading.Heading.ToString("F2"),
                ["PositionAccuracy"] = reading.PositionAccuracy.ToString("F2"),
                ["Timestamp"] = reading.Timestamp.ToString("O")
            }
        );
    }
}
