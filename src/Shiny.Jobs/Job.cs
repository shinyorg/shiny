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
        try
        {
            var fireJob = true;
            this.JobInfo = jobInfo;

            if (this.MinimumTime != null && this.LastRunTime != null)
            {
                var timeDiff = this.LastRunTime.Value.Subtract(DateTimeOffset.UtcNow);
                fireJob = timeDiff >= this.MinimumTime;
                this.Logger.LogDebug($"Time Difference: {timeDiff} - Firing Job: {fireJob}");
            }

            if (fireJob)
                await this.Run(cancelToken).ConfigureAwait(false);
        }
        finally
        {
            this.JobInfo = null;
            this.LastRunTime = DateTimeOffset.UtcNow;
        }
    }


    protected abstract Task Run(CancellationToken cancelToken);
    protected JobInfo? JobInfo { get; private set; }

    DateTimeOffset? lastRunTime;
    public DateTimeOffset? LastRunTime
    {
        get => this.lastRunTime;
        set => this.Set(ref this.lastRunTime, value);
    }


    TimeSpan? minTime;
    public TimeSpan? MinimumTime
    {
        get => this.minTime;
        set => this.Set(ref this.minTime, value);
    }
}

