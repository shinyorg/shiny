#if PLATFORM
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs.Infrastructure;


public class JobLifecycleTask : ShinyLifecycleTask, IDisposable
{
    static TimeSpan interval = TimeSpan.FromSeconds(30);
    public static TimeSpan Interval
    {
        get => interval;
        set
        {
            if (value.TotalSeconds < 15)
                throw new ArgumentException("Job foreground timer intervals cannot be less than 15 seconds");

            if (value.TotalMinutes > 5)
                throw new ArgumentException("Job foreground timer intervals cannot be greater than 5 minutes");

            interval = value;
        }
    }


    readonly ILogger logger;
    readonly IBattery battery;
    readonly IConnectivity connectivity;
    readonly IJobManager jobManager;
    readonly Timer timer;


    public JobLifecycleTask(
        ILogger<JobLifecycleTask> logger,
        IJobManager jobManager,
        IBattery battery,
        IConnectivity connectivity
    )
    {
        this.logger = logger;
        this.jobManager = jobManager;
        this.battery = battery;
        this.connectivity = connectivity;

        this.timer = new Timer();
        this.timer.Elapsed += async (sender, args) => this.RunJobs();
    }


    public override void Start()
    {
        base.Start();
        this.RunJobs(); // kick off initial timer
    }


    async Task RunJobs()
    {
        this.timer.Stop();
        this.logger.LogInformation("Starting foreground jobs");
        
        var jobs = this.jobManager
            .GetJobs()
            .Where(this.CanRun)
            .ToList();

        foreach (var job in jobs)
        {
            try
            {
                this.logger.LogInformation($"Job '{job.Identifier}' Foreground Started");
                await this.jobManager
                    .RunJobAsTask(job.Identifier)
                    .ConfigureAwait(false);

                this.logger.LogInformation($"Job '{job.Identifier}' Foreground Finished Successfully");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, $"Job '{job.Identifier}' Foreground Error");
            }
        }

        this.logger.LogInformation("Foreground jobs finished");
        if (!this.disposed && this.IsInForeground != false)
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            this.logger.LogDebug("Foreground Timer Restarting - Interval: " + Interval);
            this.timer.Start();
        }
    }


    protected override void OnStateChanged(bool backgrounding)
    {
        if (this.disposed)
            return;

        if (backgrounding)
        {
            this.logger.LogDebug("App background - stopping foreground timer");
            this.timer.Stop();
        }
        else
        {
            this.logger.LogDebug("App foreground - stopping foreground timer");
            this.timer.Interval = Interval.TotalMilliseconds;
            this.timer.Start();
        }
    }


    bool CanRun(JobInfo job)
    {
        if (!job.RunOnForeground)
            return false;

        if (!this.HasPowerLevel(job))
        {
            this.logger.LogDebug($"Job '{job.Identifier}' won't run because of insufficient power level");
            return false;
        }
        if (!this.HasReqInternet(job))
        {
            this.logger.LogDebug($"Job '{job.Identifier}' won't run because of insufficient internet requirement");
            return false;
        }
        if (!this.HasChargeStatus(job))
        {
            this.logger.LogDebug($"Job '{job.Identifier}' won't run because of insufficient charge status");
            return false;
        }
        return true;
    }


    bool HasPowerLevel(JobInfo job)
    {
        if (!job.BatteryNotLow)
            return true;

        return this.battery.Level > 20 || this.battery.IsPluggedIn();
    }


    bool HasReqInternet(JobInfo job) => job.RequiredInternetAccess switch
    {
        InternetAccess.Any => this.connectivity.IsInternetAvailable(true),
        InternetAccess.Unmetered => this.connectivity.IsInternetAvailable(false),
        InternetAccess.None => true,
        _ => false
    };


    bool HasChargeStatus(JobInfo job)
    {
        if (!job.DeviceCharging)
            return true;

        return this.battery.IsPluggedIn();
    }


    bool disposed;
    public void Dispose()
    {
        this.disposed = true;
        this.timer.Dispose();
    }
}
#endif