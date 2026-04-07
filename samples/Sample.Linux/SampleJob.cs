using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Sample.Linux;


public class SampleJob(ILogger<SampleJob> logger) : IJob
{
    public Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        logger.LogInformation("SampleJob ran at {Time}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
