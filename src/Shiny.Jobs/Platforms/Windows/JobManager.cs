using System;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Background;
using SystemCondition = Windows.ApplicationModel.Background.SystemCondition;
using SystemConditionType = Windows.ApplicationModel.Background.SystemConditionType;
using TimeTrigger = Windows.ApplicationModel.Background.TimeTrigger;
using IBackgroundTrigger = Windows.ApplicationModel.Background.IBackgroundTrigger;

namespace Shiny.Jobs;


public class JobManager(
    IServiceProvider container,
    ILogger<IJobManager> logger,
    JobRegistrar registrar
) : AbstractJobManager(container, logger, registrar), IShinyStartupTask
{
    public void Start() => this.RegisterNativeCategories();


    public override async System.Threading.Tasks.Task<AccessState> RequestAccess()
    {
        var status = await BackgroundExecutionManager.RequestAccessAsync();
        return status switch
        {
            BackgroundAccessStatus.AlwaysAllowed => AccessState.Available,
            BackgroundAccessStatus.AllowedSubjectToSystemPolicy => AccessState.Available,
            BackgroundAccessStatus.DeniedBySystemPolicy => AccessState.Restricted,
            BackgroundAccessStatus.DeniedByUser => AccessState.Denied,
            _ => AccessState.Unknown
        };
    }


    /// <summary>
    /// Registers category-based COM-activated in-proc background tasks (one per
    /// charging × network combination). Dispatch lands in <see cref="ShinyJobsBackgroundTask.Run"/>.
    /// </summary>
    internal void RegisterNativeCategories()
    {
        UnregisterAll();

        var categories = new[]
        {
            (Id: GetCategoryId(false, false), Charging: false, Network: InternetAccess.None),
            (Id: GetCategoryId(true,  false), Charging: true,  Network: InternetAccess.None),
            (Id: GetCategoryId(false, true),  Charging: false, Network: InternetAccess.Any),
            (Id: GetCategoryId(true,  true),  Charging: true,  Network: InternetAccess.Any),
        };

        foreach (var cat in categories)
        {
            var jobs = this.GetJobsByCategory(cat.Id);
            if (jobs.Count == 0)
                continue;

            var builder = new BackgroundTaskBuilder
            {
                Name = cat.Id,
                IsNetworkRequested = cat.Network != InternetAccess.None
            };
            builder.SetTrigger((IBackgroundTrigger)new TimeTrigger(15, false));

            if (cat.Network == InternetAccess.Any)
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

            // WinRT has no direct "device charging" condition. BackgroundWorkCostNotHigh
            // is the closest signal (true on AC power or with sufficient battery).
            if (cat.Charging)
                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));

            builder.SetTaskEntryPointClsid(ShinyJobsBackgroundTask.ClsidGuid);
            builder.Register();
            this.Log.LogDebug(
                "Registered Windows background task category '{Category}' with {Count} job(s)",
                cat.Id,
                jobs.Count
            );
        }
    }


    static void UnregisterAll()
    {
        foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
        {
            if (task.Name.StartsWith("com.shiny.job", StringComparison.Ordinal))
                task.Unregister(true);
        }
    }
}
