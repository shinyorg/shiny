using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs.Infrastructure
{
    public class JobLifecycleTask : ShinyLifecycleTask
    {
        static TimeSpan interval = TimeSpan.FromSeconds(60);
        public static TimeSpan Interval
        {
            get => interval;
            set
            {
                if (interval.TotalSeconds < 15)
                    throw new ArgumentException("Job foreground timer intervals cannot be less than 15 seconds");

                if (interval.TotalMinutes > 5)
                    throw new ArgumentException("Job foreground timer intervals cannot be greater than 5 minutes");

                interval = value;
            }
        }


        readonly IJobManager jobManager;
        readonly IPowerManager powerManager;
        readonly IConnectivity connectivity;
        CompositeDisposable? disposer;


        public JobLifecycleTask(IJobManager jobManager,
                                IPowerManager powerManager,
                                IConnectivity connectivity)
        {
            this.jobManager = jobManager;
            this.powerManager = powerManager;
            this.connectivity = connectivity;
        }


        public override void OnForeground()
        {
            this.disposer ??= new CompositeDisposable();
            Observable
                .Interval(Interval)
                .Select(_ => this.jobManager.GetJobs().ToObservable())
                .Switch()
                .Select(jobs => jobs.Where(job =>
                    job.RunOnForeground &&
                    this.HasPowerLevel(job) &&
                    this.HasReqInternet(job) &&
                    this.HasChargeStatus(job)
                ))
                .Select(jobs => Observable.FromAsync(async ct =>
                {
                    foreach (var job in jobs)
                    {
                        if (!ct.IsCancellationRequested)
                        {
                            // the ct doesn't really get used down in this level as it will be delegated to the background task and cancelled
                            // if ran too long
                            await this.jobManager.RunJobAsTask(job.Identifier);
                        }
                    }
                }))
                .Subscribe()
                .DisposedBy(this.disposer);
        }


        public override void OnBackground()
        {
            this.disposer?.Dispose();
            this.disposer = null;
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
                return false;

            return this.powerManager.IsPluggedIn();
        }
    }
}
