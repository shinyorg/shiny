using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Android.Content;
using AndroidX.Work;
using P = Android.Manifest.Permission;
using Microsoft.Extensions.Logging;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        readonly IAndroidContext context;


        public JobManager(IAndroidContext context,
                          IServiceProvider container,
                          IRepository repository,
                          ILogger<IJobManager> logger) : base(container, repository, logger)
        {
            this.context = context;
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
                            catch (Exception ex)
                            {
                                this.Log.LogError(ex, "Error running task - " + taskName);
                            }
                            finally
                            {
                                wakeLock.Release();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Log.LogError(ex, "Error setting up task - " + taskName);
                }
            }
        }


        protected override void RegisterNative(JobInfo jobInfo)
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

        protected override void CancelNative(JobInfo jobInfo)
            => WorkManager.Instance.CancelUniqueWork(jobInfo.Identifier);


        public override async Task CancelAll()
        {
            await base.CancelAll();
            WorkManager.Instance.CancelAllWork();
        }
    }
}
