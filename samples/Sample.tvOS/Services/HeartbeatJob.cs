using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Sample.tvOS.Services;


/// <summary>
/// A plain IJob. On tvOS this is scheduled through BGTaskScheduler exactly as it would be on iOS -
/// the identifiers it registers against are the ones listed in BGTaskSchedulerPermittedIdentifiers.
/// </summary>
public class HeartbeatJob(ILogger<HeartbeatJob> logger, AppLog log) : IJob
{
    public Task Run(CancellationToken cancelToken)
    {
        var message = $"HeartbeatJob ran at {DateTimeOffset.Now:HH:mm:ss}";
        logger.LogInformation(message);
        log.Write(message);
        return Task.CompletedTask;
    }
}
