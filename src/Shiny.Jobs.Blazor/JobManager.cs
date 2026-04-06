using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;
using Shiny.Support.Repositories;

namespace Shiny.Jobs.Blazor;


public class JobManager : AbstractJobManager, IShinyComponentStartup, IDisposable
{
    static readonly System.Collections.Generic.List<JobInfo> registeredJobs = new();
    internal static void AddJob(JobInfo jobInfo) => registeredJobs.Add(jobInfo);

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


    readonly IBattery battery;
    readonly IConnectivity connectivity;
    readonly Timer timer;
    bool disposed;


    public JobManager(
        IServiceProvider container,
        IRepository repository,
        IObjectStoreBinder storeBinder,
        ILogger<IJobManager> logger,
        IBattery battery,
        IConnectivity connectivity
    ) : base(
        container,
        repository,
        storeBinder,
        logger
    )
    {
        this.battery = battery;
        this.connectivity = connectivity;

        this.timer = new Timer();
        this.timer.Elapsed += (_, _) => this.RunForegroundJobs();
    }


    public void ComponentStart()
    {
        foreach (var job in registeredJobs)
        {
            var jobNew = job with { IsSystemJob = true };
            this.Register(jobNew);
            this.Log.LogDebug("Registered System Job '{Identifier}' of Type '{JobType}'", job.Identifier, job.JobType);
        }

        this.RunForegroundJobs();
    }


    public override Task<AccessState> RequestAccess()
        => Task.FromResult(AccessState.Available);

    protected override void CancelNative(JobInfo jobInfo) { }
    protected override void RegisterNative(JobInfo jobInfo) { }


    async void RunForegroundJobs()
    {
        this.timer.Stop();
        this.Log.LogDebug("Starting foreground jobs");

        var jobs = this.GetJobs();
        foreach (var job in jobs)
        {
            if (!this.CanRun(job))
                continue;

            try
            {
                this.Log.LogDebug("Job '{Identifier}' Foreground Started", job.Identifier);
                await this
                    .RunJobAsTask(job.Identifier)
                    .ConfigureAwait(false);

                this.Log.LogDebug("Job '{Identifier}' Foreground Finished Successfully", job.Identifier);
            }
            catch (Exception ex)
            {
                this.Log.LogWarning(ex, "Job '{Identifier}' Foreground Error", job.Identifier);
            }
        }

        this.Log.LogDebug("Foreground jobs finished");

        if (!this.disposed)
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            this.Log.LogDebug("Foreground Timer Restarting - Interval: {Interval}", Interval);
            this.timer.Start();
        }
    }


    bool CanRun(JobInfo job)
    {
        if (!job.RunOnForeground)
            return false;

        if (job.BatteryNotLow && this.battery.Level <= 20 && !this.battery.IsPluggedIn())
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient power level", job.Identifier);
            return false;
        }

        if (!this.HasReqInternet(job))
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient internet requirement", job.Identifier);
            return false;
        }

        if (job.DeviceCharging && !this.battery.IsPluggedIn())
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient charge status", job.Identifier);
            return false;
        }

        return true;
    }


    bool HasReqInternet(JobInfo job) => job.RequiredInternetAccess switch
    {
        InternetAccess.Any => this.connectivity.IsInternetAvailable(true),
        InternetAccess.Unmetered => this.connectivity.IsInternetAvailable(false),
        InternetAccess.None => true,
        _ => false
    };


    public void Dispose()
    {
        this.disposed = true;
        this.timer.Dispose();
    }
}
