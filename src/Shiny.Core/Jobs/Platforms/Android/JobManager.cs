using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Settings;
using Android;
using Android.App.Job;
using Android.Content;
using Java.Lang;
using Shiny.Logging;
using P = Android.Manifest.Permission;
using static Android.OS.PowerManager;
#if ANDROIDX
using AndroidX.Work;
#else
using JobBuilder = Android.App.Job.JobInfo.Builder;
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


        public override Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);


        public override async void RunTask(string taskName, Func<CancellationToken, Task> task)
        {
            if (!this.context.IsInManifest(P.WakeLock))
            {
                base.RunTask(taskName, task);
            }
            else
            {
                try
                {
                    using (var pm = this.context.GetSystemService<Android.OS.PowerManager>(Context.PowerService))
                    {
                        using (var wakeLock = pm.NewWakeLock(Android.OS.WakeLockFlags.Partial, "ShinyTask"))
                        {
                            try
                            {
                                wakeLock.Acquire();
                                await task(CancellationToken.None).ConfigureAwait(false);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Write(ex);
                            }
                            finally
                            {
                                wakeLock.Release();
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }

#if ANDROIDX

        protected override void ScheduleNative(JobInfo jobInfo)
        {
            this.CancelNative(jobInfo);

            //WorkManager.Initialize(this.context.AppContext, new Configuration())
            var constraints = new Constraints.Builder()
                .SetRequiresBatteryNotLow(jobInfo.BatteryNotLow)
                .SetRequiresCharging(jobInfo.DeviceCharging)
                .SetRequiredNetworkType(ToNative(jobInfo.RequiredInternetAccess))
                .Build();

            var data = new Data.Builder();
            data.PutString(ShinyJobWorker.ShinyJobIdentifier, jobInfo.Identifier);

            if (jobInfo.Repeat)
            {
                var request = new PeriodicWorkRequest.Builder(typeof(ShinyJobWorker), TimeSpan.FromMinutes(15)) 
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
                    .SetConstraints(constraints)
                    .Build();

                WorkManager.Instance.EnqueueUniqueWork(
                    jobInfo.Identifier,
                    ExistingWorkPolicy.Append,
                    worker
                );
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
            => WorkManager.Instance.CancelUniqueWork(jobInfo.Identifier);


        public override async Task CancelAll()
        {
            await base.CancelAll();
            WorkManager.Instance.CancelAllWork();
        }

#else

        protected override void ScheduleNative(JobInfo jobInfo)
        {
            this.CancelNative(jobInfo);

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
            .SetRequiresCharging(jobInfo.DeviceCharging);

            if (jobInfo.PeriodicTime != null)
            {
                if (jobInfo.PeriodicTime < TimeSpan.FromMinutes(15))
                    throw new ArgumentException("You cannot schedule periodic jobs faster than 15 minutes");

                builder.SetPeriodic(Convert.ToInt64(System.Math.Round(jobInfo.PeriodicTime.Value.TotalMilliseconds, 0)));
            }

            if (jobInfo.BatteryNotLow)
            {
                if (this.context.IsMinApiLevel(26))
                    builder.SetRequiresBatteryNotLow(jobInfo.BatteryNotLow);
                else
                    Log.Write(nameof(JobManager), "BatteryNotLow criteria is only supported on API 26+");
            }

            if (jobInfo.RequiredInternetAccess != InternetAccess.None)
            {
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
