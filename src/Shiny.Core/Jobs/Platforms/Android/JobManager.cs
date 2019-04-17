using System;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Android;
#if ANDROID9
using AndroidX.Work;
#endif

namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly AndroidContext context;


        public JobManager(AndroidContext context,
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


        #if ANDROID9

        public override async Task Schedule(JobInfo jobInfo)
        {
            await base.Schedule(jobInfo);
            //WorkManager.Initialize(this.context.AppContext, new Configuration())
            var constraints = new Constraints.Builder()
                .SetRequiresBatteryNotLow(jobInfo.BatteryNotLow)
                .SetRequiresCharging(jobInfo.DeviceCharging)
                .SetRequiredNetworkType(ToNative(jobInfo.RequiredInternetAccess))
                .Build();

            var data = new Data.Builder();
            foreach (var parameter in jobInfo.Parameters)
                data.Put(parameter.Key, parameter.Value);

            if (jobInfo.Repeat)
            {
                var request = PeriodicWorkRequest
                    .Builder
                    .From<ShinyJobWorker>(TimeSpan.FromMinutes(20))
                    .SetConstraints(constraints)
                    .SetInputData(data.Build())
                    .Build();

                WorkManager.Instance.EnqueueUniquePeriodicWork(
                    jobInfo.Identifier,
                    ExistingPeriodicWorkPolicy.Replace,
                    request
                );
            }
            else
            {
                var worker = new OneTimeWorkRequest.Builder()
                    .SetInputData(data.Build())
                    .SetConstraints(constraints);

            }
        }


        static NetworkType ToNative(InternetAccess access)
        {
            switch (access)
            {
                case InternetAccess.Any:
                    return NetworkType.Connected;

                case InternetAccess.Unmetered:
                    return NetworkType.Unmetered;

                case InternetAccess.None:
                default:
                    return NetworkType.NotRequired;
            }
        }

        public override async Task Cancel(string jobId)
        {
            await base.Cancel(jobId);
            WorkManager.Instance.CancelUniqueWork(jobId);
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            WorkManager.Instance.CancelAllWork();
        }

        #else
        public override async Task Schedule(JobInfo jobInfo)
        {
            await base.Schedule(jobInfo);
            this.context.StartJobService();
        }


        public override async Task Cancel(string jobId)
        {
            await base.Cancel(jobId);
            var jobs = await this.Repository.GetAll<JobInfo>();
            if (!jobs.Any())
                this.context.StopJobService();
        }


        public override async Task CancelAll()
        {
            await base.CancelAll();
            this.context.StopJobService();
        }

        #endif
    }
}