using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Jobs;


/// <summary>
/// Convenience base class for <see cref="IJob"/> implementations that adds optional
/// minimum-interval throttling between runs.
/// </summary>
public abstract class Job : IJob
{
    /// <summary>
    /// Logger available to derived jobs.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Creates a new instance of the job.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    protected Job(ILogger logger) => this.Logger = logger;


    /// <summary>
    /// Runs the job, respecting <see cref="MinimumTime"/> if set.
    /// </summary>
    /// <param name="cancelToken">Token used to signal cancellation.</param>
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


    /// <summary>
    /// Implements the actual work performed by the job.
    /// </summary>
    /// <param name="cancelToken">Token used to signal cancellation.</param>
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
