using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs.Infrastructure
{
    public class JobAppStateDelegate : ShinyAppStateDelegate
    {
        readonly Lazy<IJobManager> jobManager = ShinyHost.LazyResolve<IJobManager>();
        readonly Lazy<IPowerManager> powerManager = ShinyHost.LazyResolve<IPowerManager>();
        readonly Lazy<IConnectivity> connectivity = ShinyHost.LazyResolve<IConnectivity>();

        readonly string jobIdentifier;
        readonly JobForegroundRunStates? states;
        readonly TimeSpan? foregroundInterval;
        CompositeDisposable? disposer;


        public JobAppStateDelegate(string jobIdentifier,
                                   JobForegroundRunStates? states,
                                   TimeSpan? foregroundInterval)
        {
            this.jobIdentifier = jobIdentifier;
            this.states = states;
            this.foregroundInterval = foregroundInterval;
        }


        public override void OnBackground()
        {
            this.TryRun(JobForegroundRunStates.Backgrounded);
            this.disposer?.Dispose();
            this.disposer = null;
        }


        public override void OnForeground()
        {
            this.TryRun(JobForegroundRunStates.Resumed);
            this.Set();
        }


        public override void OnStart()
        {
            this.TryRun(JobForegroundRunStates.Started);
            this.Set();
        }


        void TryRun(JobForegroundRunStates current)
        {
            if (this.states?.HasFlag(current) ?? false)
                this.TryRun();
        }


        async void TryRun()
        {
            var job = await this.jobManager.Value.GetJob(this.jobIdentifier);
            if (job != null)
                await this.jobManager.Value.Run(this.jobIdentifier);
        }


        void Set()
        {
            this.disposer ??= new CompositeDisposable();

            if (this.foregroundInterval != null)
            {
                this.disposer.Add
                (
                    Observable
                        .Interval(this.foregroundInterval.Value)
                        .Subscribe(_ => this.TryRun())
                );
            }

            if (this.states != null)
            {
                if (this.states.Value.HasFlag(JobForegroundRunStates.DeviceCharging))
                {
                    this.disposer.Add
                    (
                        this.powerManager
                            .Value
                            .WhenChargingChanged()
                            .Where(x => x == true)
                            .Subscribe(x => this.TryRun())
                    );
                }

                var any = this.states.Value.HasFlag(JobForegroundRunStates.InternetAvailableAny);
                var wifi = this.states.Value.HasFlag(JobForegroundRunStates.InternetAvailableWifi);
                if (any || wifi)
                {
                    this.disposer.Add
                    (
                        this.connectivity
                            .Value
                            .WhenInternetStatusChanged()
                            .Where(x => x == true)
                            .Subscribe(x =>
                            {
                                if (this.connectivity.Value.IsDirectConnect() && wifi)
                                {
                                    this.TryRun();
                                }
                                else if (any)
                                {
                                    this.TryRun();
                                }
                            })
                    );
                }
            }
        }
    }
}
