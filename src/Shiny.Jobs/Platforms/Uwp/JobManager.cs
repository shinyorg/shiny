using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager, IBackgroundTaskProcessor
    {
        public JobManager(IServiceProvider container, IRepository repository, ILogger<IJobManager> logger) : base(container, repository, logger) {}


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


        protected override void RegisterNative(JobInfo jobInfo)
        {
            this.CancelNative(jobInfo);

            var builder = new BackgroundTaskBuilder();
            builder.Name = ToJobTaskName(jobInfo);
            builder.TaskEntryPoint = UwpPlatform.BackgroundTaskName;

            if (jobInfo.PeriodicTime != null)
            {
                if (jobInfo.PeriodicTime < TimeSpan.FromMinutes(15))
                    throw new ArgumentException("You cannot schedule periodic jobs faster than 15 minutes");

                var runMins = Convert.ToUInt32(Math.Round(jobInfo.PeriodicTime.Value.TotalMinutes, 0));
                builder.SetTrigger(new TimeTrigger(runMins, false));
            }

            if (jobInfo.RequiredInternetAccess != InternetAccess.None)
            {
                var type = jobInfo.RequiredInternetAccess == InternetAccess.Any
                    ? SystemConditionType.InternetAvailable
                    : SystemConditionType.FreeNetworkAvailable;

                builder.AddCondition(new SystemCondition(type));
            }

            // TODO: this periodically crashes
            builder.Register();
        }


        protected override void CancelNative(JobInfo jobInfo)
            => UwpPlatform.RemoveBackgroundTask(ToJobTaskName(jobInfo));


        static string ToJobTaskName(JobInfo jobInfo) => $"JOB-{jobInfo.Identifier}";
    }
}

