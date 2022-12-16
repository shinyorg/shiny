#if PLATFORM
using System;
using System.Linq;
using System.Timers;
using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs.Infrastructure;


public class JobLifecycleTask : ShinyLifecycleTask
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
    readonly Timer timer;


    public JobLifecycleTask(
        IJobManager jobManager,
        IBattery battery,
        IConnectivity connectivity
    )
    {
        this.battery = battery;
        this.connectivity = connectivity;

        this.timer = new Timer();
        this.timer.Elapsed += async (sender, args) =>
        {
            this.timer.Stop();
            var jobs = jobManager.GetJobs();
            var toRun = jobs.Where(this.CanRun).ToList();

            foreach (var job in toRun)
                await jobManager.RunJobAsTask(job.Identifier).ConfigureAwait(false);

            if (this.IsInForeground)
                this.timer.Start();
        };
    }


    protected override void OnStateChanged(bool backgrounding)
    {
        if (backgrounding)
        {
            this.timer.Stop();
        }
        else
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            this.timer.Start();
        }
    }


    bool CanRun(JobInfo job)
    {
        if (!job.RunOnForeground)
            return false;

        if (!this.HasPowerLevel(job))
            return false;

        if (!this.HasReqInternet(job))
            return false;

        if (!this.HasChargeStatus(job))
            return false;

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
        InternetAccess.Any => this.connectivity.IsInternetAvailable(),
        InternetAccess.Unmetered => !this.connectivity.Access.HasFlag(NetworkReach.ConstrainedInternet),
        InternetAccess.None => true,
        _ => false
    };


    bool HasChargeStatus(JobInfo job)
    {
        if (!job.DeviceCharging)
            return true;

        return this.battery.IsPluggedIn();
    }
}
#endif