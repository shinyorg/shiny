using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Jobs.Net;
using Shiny.Jobs.Power;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static bool AddJobs(this IServiceCollection services, bool? clearPrevJobs = null)
    {
        if (clearPrevJobs != null)
            JobsStartup.ClearJobsBeforeRegistering = clearPrevJobs.Value;

        services.AddRepository<JobInfoStoreConverter, JobInfo>();
        services.AddShinyService<JobsStartup>();
        services.TryAddSingleton<IJobManager, JobManager>();

#if IOS || ANDROID || MACCATALYST
        services.AddBattery();
        services.AddConnectivity();
        services.AddShinyService<JobLifecycleTask>();
        return true;
#else
        return false;
#endif
    }
}
