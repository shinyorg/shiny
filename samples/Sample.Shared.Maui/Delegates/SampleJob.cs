using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Jobs;

namespace Sample.Shared.Maui.Delegates;

public class SampleJob(ILogger<SampleJob> logger, IEventStore events) : IJob
{
    public async Task Run(CancellationToken cancelToken)
    {
        var started = DateTimeOffset.UtcNow;
        logger.LogInformation("SampleJob running");

        await events.Add(
            "Job",
            "SampleJob started",
            new Dictionary<string, string?>
            {
                ["Started"] = started.ToString("O")
            },
            cancelToken
        );

        await Task.Delay(TimeSpan.FromSeconds(1), cancelToken);

        await events.Add(
            "Job",
            "SampleJob completed",
            new Dictionary<string, string?>
            {
                ["Started"] = started.ToString("O"),
                ["DurationMs"] = ((long)(DateTimeOffset.UtcNow - started).TotalMilliseconds).ToString()
            },
            cancelToken
        );
        logger.LogInformation("SampleJob completed");
    }
}
