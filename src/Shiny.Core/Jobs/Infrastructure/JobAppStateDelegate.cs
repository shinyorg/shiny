﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs.Infrastructure
{
    public class JobAppStateDelegate : IAppStateDelegate
    {
        public static TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(60);
        readonly IJobManager jobManager;
        readonly IPowerManager powerManager;
        readonly IConnectivity connectivity;
        CompositeDisposable? disposer;


        public JobAppStateDelegate(IJobManager jobManager,
                                   IPowerManager powerManager,
                                   IConnectivity connectivity)
        {
            this.jobManager = jobManager;
            this.powerManager = powerManager;
            this.connectivity = connectivity;
        }


        public void OnStart()
        {
            this.Set();
            this.TryRun();
        }


        public void OnForeground()
        {
            this.Set();
            this.TryRun();
        }


        public void OnBackground()
        {
            this.disposer?.Dispose();
            this.disposer = null;
            this.TryRun();
        }


        static bool running = false;
        async Task TryRun(Func<JobInfo, bool>? predicate = null)
        {
            if (running)
                return;

            running = true;
            var jobs = await this.jobManager.GetJobs();
            if (predicate != null)
                jobs = jobs.Where(predicate);

            if (jobs.Any())
            {
                this.jobManager.RunTask(
                    nameof(JobAppStateDelegate),
                    async ct =>
                    {
                        using (ct.Register(() => running = false))
                        {
                            foreach (var job in jobs)
                                if (!ct.IsCancellationRequested)
                                    await this.jobManager.Run(job.Identifier, ct);
                        }
                    }
                );
            }
            // TODO: wait until they are all done - hmmmm
            running = false;
        }


        void Set()
        {
            this.disposer ??= new CompositeDisposable();

            this.disposer.Add
            (
                Observable
                    .Interval(Interval)
                    .Subscribe(_ => this.TryRun(x => x.RunOnForeground))
            );
            this.disposer.Add
            (
                this.powerManager
                    .WhenChargingChanged()
                    .Where(x => x == true)
                    .Subscribe(x => this.TryRun(x =>
                        x.RunOnForeground &&
                        x.DeviceCharging
                    ))
            );

            this.connectivity
                .WhenInternetStatusChanged()
                .Where(x => x == true)
                .Subscribe(x =>
                {
                    if (this.connectivity.IsDirectConnect())
                    {
                        this.TryRun(x =>
                            x.RunOnForeground &&
                            x.RunOnForeground &&
                            (
                                x.RequiredInternetAccess == InternetAccess.Any ||
                                x.RequiredInternetAccess == InternetAccess.Unmetered
                            )
                        );
                    }
                    else
                    {
                        this.TryRun(x =>
                            x.RunOnForeground &&
                            x.RequiredInternetAccess == InternetAccess.Any
                        );
                    }
                });
        }
    }
}
