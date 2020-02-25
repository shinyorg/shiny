using System;
using System.Reactive.Linq;


namespace Shiny.Jobs.Infrastructure
{
    public class JobAppStateDelegate : ShinyAppStateDelegate
    {
        readonly Lazy<IJobManager> jobManager = ShinyHost.LazyResolve<IJobManager>();
        readonly string jobIdentifier;
        readonly JobForegroundRunStates? states;
        readonly TimeSpan? foregroundInterval;
        IDisposable? timer;


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
            this.timer?.Dispose();
            this.timer = null;
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
            if (this.states?.HasFlag(JobForegroundRunStates.Started) ?? false)
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
            if (this.foregroundInterval != null)
            {
                this.timer ??= Observable
                    .Interval(this.foregroundInterval.Value)
                    .Subscribe(_ => this.TryRun());
            }
        }
    }
}
