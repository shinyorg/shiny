using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Timer = System.Timers.Timer;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly Timer timer;


        public JobManager(IServiceProvider container,
                          IRepository repository,
                          IPowerManager powerManager,
                          IConnectivity connectivity,
                          JobManagerConfig config = null) : base(container, repository, powerManager, connectivity)
        {
            config = config ?? new JobManagerConfig();
            this.timer = new Timer(config.PeriodInterval.TotalMilliseconds);
            this.timer.Elapsed += async (sender, args) =>
            {
                this.timer.Stop();
                await this.RunAll(CancellationToken.None);
                this.timer.Start();
            };
        }


        public override Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);
    }
}
