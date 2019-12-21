using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
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


        protected override void ScheduleNative(JobInfo jobInfo)
        {
            this.CancelNative(jobInfo);

            var builder = new BackgroundTaskBuilder();
            builder.Name = GetJobTaskName(jobInfo);
            builder.TaskEntryPoint = typeof(ShinyBackgroundTask).FullName;

            //public void Start() => UwpShinyHost.RegisterBackground<JobBackgroundTaskProcessor>(builder => builder.SetTrigger(new TimeTrigger(15, false)));
            // TODO: criteria
            builder.Register();
        }


        protected override void CancelNative(JobInfo jobInfo) => GetTask("JOB-" + jobInfo.Identifier)?.Unregister(false);


        static string GetJobTaskName(JobInfo job) => "JOB-" + job.Identifier;

        static IBackgroundTaskRegistration GetTask(string taskName) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(taskName))
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}

