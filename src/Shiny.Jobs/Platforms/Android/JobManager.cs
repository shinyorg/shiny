using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Work;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using P = Android.Manifest.Permission;

namespace Shiny.Jobs;

public class JobManager : AbstractJobManager
{
    readonly AndroidPlatform platform;


    public JobManager(AndroidPlatform platform,
                      IServiceProvider container,
                      IRepository repository,
                      ILogger<IJobManager> logger) : base(container, repository, logger)
    {
        this.platform = platform;
    }


    public override Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);


    public override async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        // TODO: I could run this through unique job work
        if (!this.platform.IsInManifest(P.WakeLock))
        {
            base.RunTask(taskName, task);
        }
        else
        {
            try
            {
                using var pm = this.platform.GetSystemService<Android.OS.PowerManager>(Context.PowerService);
                using var wakeLock = pm.NewWakeLock(Android.OS.WakeLockFlags.Partial, "ShinyTask");
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

            this.Instance.EnqueueUniquePeriodicWork(
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

            this.Instance.EnqueueUniqueWork(
                jobInfo.Identifier,
                ExistingWorkPolicy.Append,
                worker
            );
        }
    }


    static NetworkType ToNative(InternetAccess access) => access switch
    {
        InternetAccess.Any => NetworkType.Connected,
        InternetAccess.Unmetered => NetworkType.Unmetered,
        _ => NetworkType.NotRequired
    };


    protected override void CancelNative(JobInfo jobInfo)
        => this.Instance.CancelUniqueWork(jobInfo.Identifier);


    public override async Task CancelAll()
    {
        await base.CancelAll().ConfigureAwait(false);
        this.Instance.CancelAllWork();
    }


    WorkManager Instance => WorkManager.GetInstance(this.platform.AppContext);
}
