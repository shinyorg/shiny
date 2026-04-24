using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace Sample.Shared.Maui.Delegates;

public class SampleMotionActivityDelegate(ILogger<SampleMotionActivityDelegate> logger) : IMotionActivityDelegate
{
    public Task OnReading(MotionActivityReading reading)
    {
        logger.LogInformation("Motion Activity: {Activity}, Confidence={Confidence}",
            reading.Activity, reading.Confidence);
        return Task.CompletedTask;
    }
}
