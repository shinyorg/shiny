using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Jobs;


public abstract class Job : IJob
{
    protected ILogger Logger { get; }
    protected Job(ILogger logger) => this.Logger = logger;


    public async Task Run(CancellationToken cancelToken)
    {
        var fireJob = true;

        if (this.MinimumTime != null && this.LastRunTime != null)
        {
            var timeDiff = DateTimeOffset.UtcNow.Subtract(this.LastRunTime.Value);
            fireJob = timeDiff >= this.MinimumTime;
            this.Logger.LogDebug("Time Difference: {TimeDiff} - Firing Job: {FireJob}", timeDiff, fireJob);
        }

        if (fireJob)
        {
            this.Logger.LogDebug("Running Job");
            await this.RunJob(cancelToken).ConfigureAwait(false);
            this.LastRunTime = DateTimeOffset.UtcNow;
        }
    }


    protected abstract Task RunJob(CancellationToken cancelToken);

    /// <summary>
    /// Last runtime of this job. Null if never run before.
    /// Persists in memory for the singleton lifetime.
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Sets a minimum time between this job firing.
    /// </summary>
    public TimeSpan? MinimumTime { get; set; }
}
