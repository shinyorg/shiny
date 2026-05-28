using System;
using System.Linq;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging;
using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs;


public class JobManager : AbstractJobManager, IShinyStartupTask, IDisposable
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


    readonly IBattery battery;
    readonly IConnectivity connectivity;
    readonly JobRegistrar registrar;
    readonly Timer timer;
    bool disposed;


    public JobManager(
        IServiceProvider container,
        ILogger<IJobManager> logger,
        IBattery battery,
        IConnectivity connectivity,
        JobRegistrar registrar
    ) : base(container, logger)
    {
        this.battery = battery;
        this.connectivity = connectivity;
        this.registrar = registrar;

        this.timer = new Timer();
        this.timer.Elapsed += (_, _) => this.RunForegroundJobs();
    }


    public void Start()
    {
        this.AddRegistrations(this.registrar.Jobs);
        this.Log.LogDebug("Registered {Count} job(s)", this.registrar.Jobs.Count);

        this.RunForegroundJobs();
    }


    public override Task<AccessState> RequestAccess()
        => Task.FromResult(AccessState.Available);


    async void RunForegroundJobs()
    {
        this.timer.Stop();
        if (this.disposed)
            return;

        this.Log.LogDebug("Starting foreground jobs");

        var jobs = this.GetJobs()
            .Where(kvp => this.CanRun(kvp.Value))
            .ToList();

        foreach (var (type, reg) in jobs)
        {
            try
            {
                this.Log.LogDebug("Job '{Identifier}' Foreground Started", reg.Identifier);
                await this
                    .RunJob(type, reg, default)
                    .ConfigureAwait(false);

                this.Log.LogDebug("Job '{Identifier}' Foreground Finished Successfully", reg.Identifier);
            }
            catch (Exception ex)
            {
                this.Log.LogWarning(ex, "Job '{Identifier}' Foreground Error", reg.Identifier);
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


    bool CanRun(JobRegistration reg)
    {
        if (!reg.RunOnForeground)
            return false;

        if (reg.BatteryNotLow && this.battery.Level <= 20 && !this.battery.IsPluggedIn())
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient power level", reg.Identifier);
            return false;
        }

        if (!this.HasReqInternet(reg))
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient internet requirement", reg.Identifier);
            return false;
        }

        if (reg.DeviceCharging && !this.battery.IsPluggedIn())
        {
            this.Log.LogDebug("Job '{Identifier}' won't run because of insufficient charge status", reg.Identifier);
            return false;
        }

        return true;
    }


    bool HasReqInternet(JobRegistration reg) => reg.RequiredInternetAccess switch
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
