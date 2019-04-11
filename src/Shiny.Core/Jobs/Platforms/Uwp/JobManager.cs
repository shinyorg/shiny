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
            this.TryRegUwpJob();
            await base.Schedule(jobInfo);
        }


        public override async Task Cancel(string jobName)
        {
            await base.Cancel(jobName);
            var jobs = await this.GetJobs();
            if (!jobs.Any())
                GetPluginJob()?.Unregister(true);
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            GetPluginJob()?.Unregister(true);
        }


        void TryRegUwpJob()
        {
            // TODO: do I have to reg every app start?
            var job = GetPluginJob();
            if (job == null)
            {
                var builder = new BackgroundTaskBuilder
                {
                    Name = JobBackgroundTask.BackgroundJobName,
                    //CancelOnConditionLoss = false,
                    //IsNetworkRequested = true,
                    TaskEntryPoint = typeof(JobBackgroundTask).FullName
                };
                //builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable));
                //builder.SetTrigger(new BluetoothLEAdvertisementWatcherTrigger());
                //builder.SetTrigger(new GattServiceProviderTrigger());
                //builder.SetTrigger(new GeovisitTrigger());
                //builder.SetTrigger(new ToastNotificationActionTrigger());
                if (JobBackgroundTask.PeriodicRunTime.TotalSeconds < 15)
                    throw new ArgumentException("Background timer cannot be less than 15mins");

                var runMins = Convert.ToUInt32(Math.Round(JobBackgroundTask.PeriodicRunTime.TotalMinutes, 0));
                builder.SetTrigger(new TimeTrigger(runMins, false));
                builder.Register();
            }
        }


        static IBackgroundTaskRegistration GetPluginJob()
            => BackgroundTaskRegistration
                .AllTasks
                .Where(x => x.Value.Name.Equals(JobBackgroundTask.BackgroundJobName))
                .Select(x => x.Value)
                .FirstOrDefault();
    }
}
