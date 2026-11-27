#if PLATFORM
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs.Infrastructure;


public class JobLifecycleTask(
    ILogger<JobLifecycleTask> logger,
    IJobManager jobManager,
    IBattery battery,
    IConnectivity connectivity
) : ShinyLifecycleTask, IDisposable
{
    readonly Timer timer = new();

    public static TimeSpan Interval
    {
        get;
        set
        {
            if (value.TotalSeconds < 15)
                throw new ArgumentException("Job foreground timer intervals cannot be less than 15 seconds");

            if (value.TotalMinutes > 5)
                throw new ArgumentException("Job foreground timer intervals cannot be greater than 5 minutes");

            field = value;
        }
    } = TimeSpan.FromMinutes(1);


    public override void Start()
    {
        base.Start();
        this.timer.Elapsed += (_, _) => _ = this.RunJobs();

        logger.LogDebug("Registered {Count} job(s)", jobManager.GetJobs().Count);

        // a background launch (BGTask, WorkManager, silent push...) is already running the jobs through the OS
        // scheduler - running the foreground batch now would run them twice. Wait for the first foreground instead.
        if (LaunchState.IsLaunchedInBackground())
        {
            logger.LogDebug("Process launched in the background - foreground jobs will run on the first foreground");
            Volatile.Write(ref this.runOnFirstForeground, true);
        }
        else
        {
            _ = this.RunJobs();
        }
    }


    bool runOnFirstForeground;


    int running;
    async Task RunJobs()
    {
        this.timer.Stop();

        // the timer, the foreground restart and Start can all ask at once - only one batch runs at a time,
        // so a singleton job is never run concurrently by this runner
        if (Interlocked.CompareExchange(ref this.running, 1, 0) != 0)
            return;

        try
        {
            logger.LogDebug("Starting foreground jobs");

            var jobs = jobManager
                .GetJobs()
                .Where(kvp => this.CanRun(kvp.Key, kvp.Value))
                .ToList();

            foreach (var (type, reg) in jobs)
            {
                try
                {
                    logger.LogDebug("Job '{JobType}' Foreground Started", type.FullName);

                    if (jobManager is AbstractJobManager abstractManager)
                        await abstractManager.RunJob(type, reg, CancellationToken.None).ConfigureAwait(false);

                    logger.LogDebug("Job '{JobType}' Foreground Finished Successfully", type.FullName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Job '{JobType}' Foreground Error", type.FullName);
                }
            }

            logger.LogDebug("Foreground jobs finished");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating foreground jobs");
        }
        finally
        {
            Interlocked.Exchange(ref this.running, 0);
        }

        // IsInForeground is null until the first lifecycle event - keep the timer going then, but never
        // once the app has been backgrounded (a foreground service can keep the process alive indefinitely)
        if (!this.disposed && this.IsInForeground != false)
            this.StartTimer();
    }


    void StartTimer()
    {
        this.timer.Stop();
        this.timer.Interval = Interval.TotalMilliseconds;
        logger.LogDebug("Foreground Timer Restarting - Interval: {Interval}", Interval);
        this.timer.Start();
    }


    protected override void OnStateChanged(bool backgrounding)
    {
        if (this.disposed)
            return;

        if (backgrounding)
        {
            logger.LogDebug("App moving to background - stopping foreground timer");
            this.timer.Stop();
        }
        else if (Interlocked.Exchange(ref this.runOnFirstForeground, false))
        {
            // Start skipped the initial run because of a background launch - run now, which restarts the timer
            logger.LogDebug("App foreground after a background launch - running foreground jobs");
            _ = this.RunJobs();
        }
        else if (Volatile.Read(ref this.running) == 0)
        {
            // a batch still in progress restarts the timer itself when it finishes
            logger.LogDebug("App foreground - starting foreground timer");
            this.StartTimer();
        }
    }


    bool CanRun(Type jobType, JobRegistration reg)
    {
        if (!reg.RunOnForeground)
            return false;

        if (!JobConstraints.HasPowerLevel(reg, battery))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient power level", jobType);
            return false;
        }
        if (!JobConstraints.HasReqInternet(reg, connectivity))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient internet requirement", jobType);
            return false;
        }
        if (!JobConstraints.HasChargeStatus(reg, battery))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient charge status", jobType);
            return false;
        }
        return true;
    }


    bool disposed;
    public void Dispose()
    {
        this.disposed = true;
        this.timer.Dispose();
    }
}
#endif
