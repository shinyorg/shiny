using System;
using System.Linq;
using System.Timers;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs.Infrastructure
{
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


        readonly IPowerManager powerManager;
        readonly IConnectivity connectivity;
        readonly Timer timer;


        public JobLifecycleTask(IJobManager jobManager,
                                IPowerManager powerManager,
                                IConnectivity connectivity)
        {
            this.powerManager = powerManager;
            this.connectivity = connectivity;

            this.timer = new Timer();
            this.timer.Elapsed += async (sender, args) =>
            {
                this.timer.Stop();
                var jobs = await jobManager.GetJobs();
                var toRun = jobs.Where(this.CanRun).ToList();

                foreach (var job in toRun)
                    await jobManager.RunJobAsTask(job.Identifier);

                if (this.IsInForeground)
                    this.timer.Start();
            };
        }


        public override void Start()
        {

        }


        public override void OnForeground()
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            this.timer.Start();
        }


        public override void OnBackground() => this.timer.Stop();


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

            return this.powerManager.BatteryLevel > 20 || this.powerManager.IsPluggedIn();
        }


        bool HasReqInternet(JobInfo job) => job.RequiredInternetAccess switch
        {
            InternetAccess.Any => this.connectivity.IsInternetAvailable(),
            InternetAccess.Unmetered => !this.connectivity.Reach.HasFlag(NetworkReach.ConstrainedInternet),
            InternetAccess.None => true,
            _ => false
        };


        bool HasChargeStatus(JobInfo job)
        {
            if (!job.DeviceCharging)
                return true;

            return this.powerManager.IsPluggedIn();
        }
    }
}
