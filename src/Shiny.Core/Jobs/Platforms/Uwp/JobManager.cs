using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager, IBackgroundTaskProcessor
    {
        public JobManager(IServiceProvider container, IRepository repository) : base(container, repository) {}


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


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                using (var cancelSrc = new CancellationTokenSource())
                {
                    taskInstance.Canceled += (sender, args) => cancelSrc.Cancel();
                    var jobId = taskInstance.Task.Name.Replace("JOB-", String.Empty);
                    await this.Run(jobId, cancelSrc.Token);
                }
            }
            finally
            {
                deferral.Complete();
            }
        }


        protected override void ScheduleNative(JobInfo jobInfo)
        {
            this.CancelNative(jobInfo);

            var builder = new BackgroundTaskBuilder();
            builder.Name = GetJobTaskName(jobInfo);
            builder.TaskEntryPoint = UwpPlatform.BackgroundTaskName;

            if (jobInfo.PeriodicTime != null)
            {
                if (jobInfo.PeriodicTime < TimeSpan.FromMinutes(15))
                    throw new ArgumentException("You cannot schedule periodic jobs faster than 15 minutes");

                var runMins = Convert.ToUInt32(Math.Round(jobInfo.PeriodicTime.Value.TotalMinutes, 0));
                builder.SetTrigger(new TimeTrigger(runMins, false));
            }

            //SystemTriggerType.PowerStateChange
            // TODO: idle, power change, etc
            if (jobInfo.RequiredInternetAccess != InternetAccess.None)
            {
                var type = jobInfo.RequiredInternetAccess == InternetAccess.Any
                    ? SystemConditionType.InternetAvailable
                    : SystemConditionType.FreeNetworkAvailable;

                builder.AddCondition(new SystemCondition(type));
            }
            builder.Register();
        }


        protected override void CancelNative(JobInfo jobInfo) => GetTask("JOB-" + jobInfo.Identifier)?.Unregister(true);

        static string GetJobTaskName(JobInfo job) => "JOB-" + job.Identifier;

        static IBackgroundTaskRegistration GetTask(string taskName)
        {
            var tasks = BackgroundTaskRegistration
                .AllTasks
                .ToList();

            return tasks
                .Where(x => x.Value.Name.Equals(taskName))
                .Select(x => x.Value)
                .FirstOrDefault();
        }
    }
}

