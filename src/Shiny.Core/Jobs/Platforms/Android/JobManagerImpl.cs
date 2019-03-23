using System;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Android;
using Android.App.Job;
using Android.Content;
using Java.Lang;


namespace Shiny.Jobs
{
    public class JobManagerImpl : AbstractJobManager
    {
        readonly IAndroidContext context;


        public JobManagerImpl(IAndroidContext context,
                              IServiceProvider container,
                              IRepository repository,
                              IPowerManager powerManager,
                              IConnectivity connectivity) : base(container, repository, powerManager, connectivity)
        {
            this.context = context;
        }


        public override Task<AccessState> RequestAccess()
        {
            var permission = AccessState.Available;

            if (!this.context.IsInManifest(Manifest.Permission.AccessNetworkState, false))
                permission = AccessState.NotSetup;

            if (!this.context.IsInManifest(Manifest.Permission.BatteryStats, false))
                permission = AccessState.NotSetup;

            //if (!this.context.IsInManifest(Manifest.Permission.ReceiveBootCompleted, false))
            //    permission = AccessState.NotSetup;

            return Task.FromResult(permission);
        }


        public override async Task Schedule(JobInfo jobInfo)
        {
            await base.Schedule(jobInfo);
            this.StartJobService();
        }


        public override async Task Cancel(string jobId)
        {
            await base.Cancel(jobId);
            var jobs = await this.Repository.GetAll<JobInfo>();
            if (!jobs.Any())
                this.StopJobService();
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            this.StopJobService();
        }


        JobScheduler NativeScheduler() => (JobScheduler)this.context.AppContext.GetSystemService(JobService.JobSchedulerService);
        public static int AndroidJobId { get; set; } = 100;
        public static TimeSpan PeriodicRunTime { get; set; } = TimeSpan.FromMinutes(10);


        void StopJobService() => this.NativeScheduler().Cancel(AndroidJobId);
        void StartJobService()
        {
            var sch = this.NativeScheduler();
            if (!sch.AllPendingJobs.Any(x => x.Id == AndroidJobId))
            {
                var job = new Android.App.Job.JobInfo.Builder(
                        AndroidJobId,
                        new ComponentName(
                            this.context.AppContext,
                            Class.FromType(typeof(ShinyJobService))
                        )
                    )
                    .SetPeriodic(Convert.ToInt64(PeriodicRunTime.TotalMilliseconds))
                    .SetPersisted(true)
                    .Build();

                sch.Schedule(job);
            }
        }
    }
}