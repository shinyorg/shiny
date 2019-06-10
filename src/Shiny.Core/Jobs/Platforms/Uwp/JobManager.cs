using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly UwpContext context;
        public static TimeSpan PeriodicRunTime { get; set; } = TimeSpan.FromMinutes(15);


        public JobManager(UwpContext context,
                          IServiceProvider container,
                          IRepository repository,
                          IPowerManager powerManager,
                          IConnectivity connectivity) : base(container, repository, powerManager, connectivity)
        {
            this.context = context;

        }


        public override async Task<AccessState> RequestAccess()
        {
            var requestStatus = await BackgroundExecutionManager.RequestAccessAsync();
            switch (requestStatus)
            {
                //case BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity:
                //case BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity:
                case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                case BackgroundAccessStatus.AlwaysAllowed:
                    return AccessState.Available;

                default:
                    return AccessState.Denied;
                    //throw new ArgumentException("Request declined - " + requestStatus);
            }
        }


        public override async Task Schedule(JobInfo jobInfo)
        {
            if (PeriodicRunTime.TotalSeconds < 15)
                throw new ArgumentException("Background timer cannot be less than 15mins");

            var runMins = Convert.ToUInt32(Math.Round(PeriodicRunTime.TotalMinutes, 0));
            this.context.RegisterBackground<JobBackgroundTaskProcessor>(new TimeTrigger(runMins, false));
            await base.Schedule(jobInfo);
        }


        public override async Task Cancel(string jobName)
        {
            await base.Cancel(jobName);
            var jobs = await this.GetJobs();
            if (!jobs.Any())
                this.context.UnRegisterBackground<JobBackgroundTaskProcessor>();
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            this.context.UnRegisterBackground<JobBackgroundTaskProcessor>();
        }
    }
}
