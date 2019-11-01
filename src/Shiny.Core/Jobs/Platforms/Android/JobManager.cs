using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Settings;
using Android;
using Android.OS;
using Android.App.Job;
using Android.Content;
using Java.Lang;
using JobBuilder = Android.App.Job.JobInfo.Builder;
#if ANDROIDX
using AndroidX.Work;
#endif

namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly AndroidContext context;
        readonly ISettings settings;


        public JobManager(AndroidContext context,
                          IServiceProvider container,
                          IRepository repository,
                          ISettings settings) : base(container, repository, TimeSpan.FromSeconds(30))
        {
            this.context = context;
            this.settings = settings;
        }


        public override Task<AccessState> RequestAccess()
        {
            var permission = AccessState.Available;

            if (!this.context.IsInManifest(Manifest.Permission.AccessNetworkState))
                permission = AccessState.NotSetup;

            if (!this.context.IsInManifest(Manifest.Permission.BatteryStats))
                permission = AccessState.NotSetup;

            //if (!this.context.IsInManifest(Manifest.Permission.ReceiveBootCompleted, false))
            //    permission = AccessState.NotSetup;

            return Task.FromResult(permission);
        }


#if ANDROIDX

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

        protected override void ScheduleNative(JobInfo jobInfo)
        {
            var newJobId = this.settings.IncrementValue("JobId");
            var builder = new JobBuilder(
                newJobId,
                new ComponentName(
                    context.AppContext,
                    Class.FromType(typeof(ShinyJobService))
                )
            )
            .SetPersisted(true);

            var bundle = new PersistableBundle();
            bundle.PutString("ShinyJobId", jobInfo.Identifier);
            builder.SetExtras(bundle);
            builder.SetPeriodic(Convert.ToInt64(jobInfo.PeriodicTime.TotalMilliseconds));

            if (jobInfo.BatteryNotLow)
                builder.SetRequiresBatteryNotLow(true);

            if (jobInfo.DeviceCharging)
                builder.SetRequiresCharging(true);

            if (jobInfo.RequiredInternetAccess != InternetAccess.None)
            {
                //builder.SetRequiredNetwork(new Android.Net.NetworkRequest { }.HasCapability(Android.Net.NetCapability.Internet).)
                var networkType = jobInfo.RequiredInternetAccess == InternetAccess.Unmetered
                    ? NetworkType.Unmetered
                    : NetworkType.Any;
                builder.SetRequiredNetworkType(networkType);
            }

            this.context.Native().Schedule(builder.Build());
        }


        protected override void CancelNative(JobInfo jobInfo)
        {
            var native = this.context.Native();
            var nativeJob = native.GetNativeJobByShinyId(jobInfo.Identifier);

            if (nativeJob != null)
                native.Cancel(nativeJob.Id);
        }

#endif
    }
}