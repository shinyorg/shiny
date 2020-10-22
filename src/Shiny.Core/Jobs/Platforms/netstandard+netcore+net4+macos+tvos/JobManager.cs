using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Timer = System.Timers.Timer;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly Timer timer;


        public JobManager(IServiceProvider container, IRepository repository) : base(container, repository)
        {
            this.timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
            this.timer.Elapsed += async (sender, args) =>
            {
                this.timer.Stop();
                await this.RunAll(CancellationToken.None, false);
                this.timer.Start();
            };
        }


        protected override void ScheduleNative(JobInfo jobInfo) { }
        protected override void CancelNative(JobInfo jobInfo) { }
        public override Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);
    }
}
