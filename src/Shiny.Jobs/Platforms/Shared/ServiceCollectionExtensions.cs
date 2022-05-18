using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Jobs.Net;
using Shiny.Jobs.Power;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services, bool? clearPrevJobs = null)
    {
        if (clearPrevJobs != null)
            JobsStartup.ClearJobsBeforeRegistering = clearPrevJobs.Value;

        services.TryAddSqliteRepository();
        services.AddShinyService<JobsStartup>();
        //services.AddShinyService<JobLifecycleTask>(); // TODO
        services.TryAddSingleton<IJobManager, JobManager>();

#if IOS || ANDROID || MACCATALYST
        services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
        services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
#else
        services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
#endif
        return services;
    }
}
