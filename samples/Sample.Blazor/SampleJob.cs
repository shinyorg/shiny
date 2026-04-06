using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Sample.Blazor;


public class SampleJob(ILogger<SampleJob> logger) : IJob
{
    public Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        logger.LogInformation("SampleJob ran at {Time}", System.DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
