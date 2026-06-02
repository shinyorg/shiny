using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Locations;

namespace Sample.Shared.Maui.Delegates;

public class SampleMotionActivityDelegate(
    ILogger<SampleMotionActivityDelegate> logger,
    IEventStore events
) : IMotionActivityDelegate
{
    public Task OnReading(MotionActivityReading reading)
    {
        logger.LogInformation("Motion Activity: {Activity}, Confidence={Confidence}",
            reading.Activity, reading.Confidence);

        return events.Add(
            "MotionActivity",
            $"{reading.Activity} ({reading.Confidence})",
            new Dictionary<string, string?>
            {
                ["Activity"] = reading.Activity.ToString(),
                ["Confidence"] = reading.Confidence.ToString(),
                ["Timestamp"] = reading.Timestamp.ToString("O")
            }
        );
    }
}
