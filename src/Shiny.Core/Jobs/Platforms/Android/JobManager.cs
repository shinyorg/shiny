using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Settings;
using Android;
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
                          ISettings settings) : base(container, repository)
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

        protected override void ScheduleNative(JobInfo jobInfo)
        {
            //WorkManager.Initialize(this.context.AppContext, new Configuration())
            var constraints = new Constraints.Builder()
                .SetRequiresBatteryNotLow(jobInfo.BatteryNotLow)
                .SetRequiresCharging(jobInfo.DeviceCharging)
                .SetRequiredNetworkType(ToNative(jobInfo.RequiredInternetAccess))
                .Build();

            var data = new Data.Builder();
            //foreach (var parameter in jobInfo.Parameters)
            //    data.Put(parameter.Key, parameter.Value);

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
                var worker = new OneTimeWorkRequest.Builder(typeof(ShinyJobWorker))
                    .SetInputData(data.Build())
                    .SetConstraints(constraints);

            }
        }


        static AndroidX.Work.NetworkType ToNative(InternetAccess access)
        {
            switch (access)
            {
                case InternetAccess.Any:
                    return AndroidX.Work.NetworkType.Connected;

                case InternetAccess.Unmetered:
                    return AndroidX.Work.NetworkType.Unmetered;

                case InternetAccess.None:
                default:
                    return AndroidX.Work.NetworkType.NotRequired;
            }
        }

        protected override void CancelNative(JobInfo jobInfo)
        {
            WorkManager.Instance.CancelUniqueWork(jobInfo.Identifier);
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
            .SetShinyIdentifier(jobInfo.Identifier)
            .SetPersisted(true)
            .SetRequiresBatteryNotLow(jobInfo.BatteryNotLow)
            .SetRequiresCharging(jobInfo.DeviceCharging);

            if (jobInfo.PeriodicTime != null)
            {
                if (jobInfo.PeriodicTime < TimeSpan.FromMinutes(15))
                    throw new ArgumentException("You cannot schedule periodic jobs faster than 15 minutes");

                builder.SetPeriodic(Convert.ToInt64(System.Math.Round(jobInfo.PeriodicTime.Value.TotalMilliseconds, 0)));
            }

            if (jobInfo.RequiredInternetAccess != InternetAccess.None)
            {
                //builder.SetRequiredNetwork(new Android.Net.NetworkRequest { }.HasCapability(Android.Net.NetCapability.Internet).)
                var networkType = jobInfo.RequiredInternetAccess == InternetAccess.Unmetered
                    ? NetworkType.Unmetered
                    : NetworkType.Any;
                builder.SetRequiredNetworkType(networkType);
            }

            var nativeJob = builder.Build();
            this.context.Native().Schedule(nativeJob);
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
