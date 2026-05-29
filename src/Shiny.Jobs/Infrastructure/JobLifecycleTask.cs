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
        _ = this.RunJobs();
    }


    async Task RunJobs()
    {
        this.timer.Stop();
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

        if (!this.disposed)
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            logger.LogDebug("Foreground Timer Restarting - Interval: {Interval}", Interval);
            this.timer.Start();
        }
    }


    protected override void OnStateChanged(bool backgrounding)
    {
        if (this.disposed)
            return;

        if (backgrounding)
        {
            logger.LogDebug("App moving to background - timer will likely be trimmed");
        }
        else
        {
            logger.LogDebug("App foreground - starting foreground timer");
            this.timer.Stop();
            this.timer.Interval = Interval.TotalMilliseconds;
            this.timer.Start();
        }
    }


    bool CanRun(Type jobType, JobRegistration reg)
    {
        if (!reg.RunOnForeground)
            return false;

        if (!this.HasPowerLevel(reg))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient power level", jobType);
            return false;
        }
        if (!this.HasReqInternet(reg))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient internet requirement", jobType);
            return false;
        }
        if (!this.HasChargeStatus(reg))
        {
            logger.LogDebug("Job '{Type}' won't run because of insufficient charge status", jobType);
            return false;
        }
        return true;
    }


    bool HasPowerLevel(JobRegistration reg)
    {
        if (!reg.BatteryNotLow)
            return true;
        return battery.Level > 20 || battery.IsPluggedIn();
    }


    bool HasReqInternet(JobRegistration reg) => reg.RequiredInternetAccess switch
    {
        InternetAccess.Any => connectivity.IsInternetAvailable(true),
        InternetAccess.Unmetered => connectivity.IsInternetAvailable(false),
        InternetAccess.None => true,
        _ => false
    };


    bool HasChargeStatus(JobRegistration reg)
    {
        if (!reg.DeviceCharging)
            return true;
        return battery.IsPluggedIn();
    }


    bool disposed;
    public void Dispose()
    {
        this.disposed = true;
        this.timer.Dispose();
    }
}
#endif
