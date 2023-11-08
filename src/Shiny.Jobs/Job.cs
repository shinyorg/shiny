using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Jobs;


public abstract class Job : NotifyPropertyChanged, IJob
{
    protected ILogger Logger { get; }
    protected Job(ILogger logger) => this.Logger = logger;


    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        var fireJob = true;
        this.JobInfo = jobInfo;
 
        if (this.MinimumTime != null && this.LastRunTime != null)
        {
            var timeDiff = DateTimeOffset.UtcNow.Subtract(this.LastRunTime.Value);
            fireJob = timeDiff >= this.MinimumTime;
            this.Logger.LogDebug($"Time Difference: {timeDiff} - Firing Job: {fireJob}");
        }

        if (fireJob)
        {
            this.Logger.LogDebug("Running Job");
            await this.Run(cancelToken).ConfigureAwait(false);

            // if the job errors, we will keep trying
            this.LastRunTime = DateTimeOffset.UtcNow;
        }
    }


    protected abstract Task Run(CancellationToken cancelToken);
    protected JobInfo JobInfo { get; private set; }

    /// <summary>
    /// Last runtime of this job.  Null if never run before.
    /// This value will persist between runs, it is not recommended that you set this yourself
    /// </summary>
    DateTimeOffset? lastRunTime;
    public DateTimeOffset? LastRunTime
    {
        get => this.lastRunTime;
        set => this.Set(ref this.lastRunTime, value);
    }


    /// <summary>
    /// Sets a minimum time between this job firing
    /// CAREFUL: jobs tend to NOT run when the users phone is not in use
    /// This value will persist between runs
    /// </summary>
    TimeSpan? minTime;
    public TimeSpan? MinimumTime
    {
        get => this.minTime;
        set => this.Set(ref this.minTime, value);
    }
}

