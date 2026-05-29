using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Work;
using Microsoft.Extensions.Logging;
using P = Android.Manifest.Permission;

namespace Shiny.Jobs;


public class JobManager(
    AndroidPlatform platform,
    IServiceProvider container,
    ILogger<IJobManager> logger,
    JobRegistrar registrar
) : AbstractJobManager(container, logger, registrar), IShinyStartupTask
{
    public void Start() => this.RegisterNativeCategories();


    public override Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);


    public override async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        if (!platform.IsInManifest(P.WakeLock))
        {
            base.RunTask(taskName, task);
        }
        else
        {
            try
            {
                using var pm = platform.GetSystemService<Android.OS.PowerManager>(Context.PowerService);
                using var wakeLock = pm.NewWakeLock(Android.OS.WakeLockFlags.Partial, "ShinyTask");
                try
                {
                    wakeLock.Acquire();
                    await task(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.Log.LogError(ex, "Error running task - {TaskName}", taskName);
                }
                finally
                {
                    wakeLock.Release();
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex, "Error setting up task - {TaskName}", taskName);
            }
        }
    }


    protected override async Task<JobRunResult> RunAsTask(Type jobType, JobRegistration reg, CancellationToken cancellationToken)
    {
        if (!platform.IsInManifest(P.WakeLock))
            return await base.RunAsTask(jobType, reg, cancellationToken).ConfigureAwait(false);

        using var pm = platform.GetSystemService<Android.OS.PowerManager>(Context.PowerService);
        using var wakeLock = pm.NewWakeLock(Android.OS.WakeLockFlags.Partial, "ShinyJob");
        try
        {
            wakeLock.Acquire();
            return await this.RunJob(jobType, reg, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            wakeLock.Release();
        }
    }


    /// <summary>
    /// Registers category-based periodic workers matching the iOS BGTask pattern.
    /// Called once at startup after all jobs have been added.
    /// </summary>
    internal void RegisterNativeCategories()
    {
        // Cancel all existing Shiny work first
        this.Instance.CancelAllWorkByTag("com.shiny.job");

        var categories = new[]
        {
            (Id: GetCategoryId(false, false), Charging: false, Network: InternetAccess.None),
            (Id: GetCategoryId(true, false), Charging: true, Network: InternetAccess.None),
            (Id: GetCategoryId(false, true), Charging: false, Network: InternetAccess.Any),
            (Id: GetCategoryId(true, true), Charging: true, Network: InternetAccess.Any),
        };

        foreach (var cat in categories)
        {
            var jobs = this.GetJobsByCategory(cat.Id);
            if (jobs.Count == 0)
                continue;

            var constraints = new Constraints.Builder()
                .SetRequiresCharging(cat.Charging)
                .SetRequiredNetworkType(ToNative(cat.Network))
                .Build();

            var data = new Data.Builder();
            data.PutString(ShinyJobWorker.ShinyCategoryIdentifier, cat.Id);

            var request = new PeriodicWorkRequest.Builder(typeof(ShinyJobWorker), TimeSpan.FromMinutes(15))
                .SetConstraints(constraints)
                .SetInputData(data.Build())
                .AddTag("com.shiny.job")
                .Build();

            this.Instance.EnqueueUniquePeriodicWork(
                cat.Id,
                ExistingPeriodicWorkPolicy.Replace!,
                request
            );

            this.Log.LogDebug(
                "Registered Android Worker category '{Category}' with {Count} job(s)",
                cat.Id,
                jobs.Count
            );
        }
    }


    WorkManager Instance => WorkManager.GetInstance(platform.AppContext);
    static NetworkType ToNative(InternetAccess access) => access switch
    {
        InternetAccess.Any => NetworkType.Connected!,
        InternetAccess.Unmetered => NetworkType.Unmetered!,
        _ => NetworkType.NotRequired!
    };
}
