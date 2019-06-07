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
        public JobManager(IServiceProvider container,
                          IRepository repository,
                          IPowerManager powerManager,
                          IConnectivity connectivity) : base(container, repository, powerManager, connectivity)
        {
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
            UwpShinyHost.TryRegister(typeof(JobBackgroundTask), builder =>
            {
                if (JobBackgroundTask.PeriodicRunTime.TotalSeconds < 15)
                    throw new ArgumentException("Background timer cannot be less than 15mins");

                var runMins = Convert.ToUInt32(Math.Round(JobBackgroundTask.PeriodicRunTime.TotalMinutes, 0));
                builder.SetTrigger(new TimeTrigger(runMins, false));
            });
            await base.Schedule(jobInfo);
        }


        public override async Task Cancel(string jobName)
        {
            await base.Cancel(jobName);
            var jobs = await this.GetJobs();
            if (!jobs.Any())
                UwpShinyHost.TryUnRegister(typeof(JobBackgroundTask));
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            UwpShinyHost.TryUnRegister(typeof(JobBackgroundTask));
        }
    }
}
