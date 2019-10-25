using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly UwpContext context;


        public JobManager(UwpContext context, IServiceProvider container, IRepository repository) : base(container, repository, TimeSpan.FromMinutes(15))
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
            }
        }


        public override async Task Schedule(JobInfo jobInfo)
        {
            if (jobInfo.PeriodicTime != null && jobInfo.PeriodicTime < this.MinimumAllowedPeriodicTime)
                throw new ArgumentException($"Background timer cannot be less than {this.MinimumAllowedPeriodicTime.Value.TotalMinutes} minutes");

            this.context.RegisterBackground<JobBackgroundTaskProcessor>(jobInfo.Identifier, builder =>
            {
                if (jobInfo.PeriodicTime != null)
                {
                    var runMins = Convert.ToUInt32(jobInfo.PeriodicTime.Value.TotalMinutes, 0);
                    builder.SetTrigger(new TimeTrigger(runMins, false));
                }
                if (jobInfo.RequiredInternetAccess != InternetAccess.None)
                {
                    var type = jobInfo.RequiredInternetAccess == InternetAccess.Any
                        ? SystemConditionType.InternetAvailable
                        : SystemConditionType.FreeNetworkAvailable;

                     builder.AddCondition(new SystemCondition(type));
                }
            });
            await base.Schedule(jobInfo);
        }


        public override async Task Cancel(string jobName)
        {
            this.context.UnRegisterBackground<JobBackgroundTaskProcessor>(jobName);
            await this.Repository.Remove<JobInfo>(jobName);
        }


        public override async Task CancelAll()
        {
            var jobs = await this.Repository.GetAllWithKeys<JobInfo>();
            foreach (var job in jobs)
            {
                if (!job.Value.IsSystemJob)
                {
                    this.context.UnRegisterBackground<JobBackgroundTaskProcessor>(job.Key);
                    await this.Repository.Remove<JobInfo>(job.Key);
                }
            }
        }
    }
}

