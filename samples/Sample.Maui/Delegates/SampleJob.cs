using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Sample.Maui.Delegates;

public class SampleJob(ILogger<SampleJob> logger) : IJob
{
    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        logger.LogInformation("Job {Identifier} running", jobInfo.Identifier);
        await Task.Delay(TimeSpan.FromSeconds(3), cancelToken);
        logger.LogInformation("Job {Identifier} completed", jobInfo.Identifier);
    }
}
