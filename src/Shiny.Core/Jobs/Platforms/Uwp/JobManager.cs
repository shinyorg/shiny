using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager, IShinyStartupTask
    {
        public JobManager(IServiceProvider container, IRepository repository) : base(container, repository, TimeSpan.FromMinutes(15))
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
            }
        }


        protected override void ScheduleNative(JobInfo jobInfo) {}
        protected override void CancelNative(JobInfo jobInfo) { }
        public void Start() => UwpShinyHost.RegisterBackground<JobBackgroundTaskProcessor>(builder => builder.SetTrigger(new TimeTrigger(15, false)));
    }
}

