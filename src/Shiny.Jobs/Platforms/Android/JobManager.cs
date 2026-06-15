using System;
using System.Threading.Tasks;
using AndroidX.Work;
using Microsoft.Extensions.Logging;

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


    /// <summary>
    /// Registers category-based periodic workers matching the iOS BGTask pattern.
    /// Called once at startup after all jobs have been added.
    /// </summary>
    internal void RegisterNativeCategories()
    {
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
            {
                // Clean up a schedule left over from a previous run that registered jobs in this category
                this.Instance.CancelUniqueWork(cat.Id);
            }
            else
            {
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

                // Update applies constraint changes without resetting the period clock or
                // cancelling an in-flight/due execution - critical when WorkManager itself
                // started this process to run a due job
                this.Instance.EnqueueUniquePeriodicWork(
                    cat.Id,
                    ExistingPeriodicWorkPolicy.Update!,
                    request
                );

                this.Log.LogDebug(
                    "Registered Android Worker category '{Category}' with {Count} job(s)",
                    cat.Id,
                    jobs.Count
                );
            }
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
