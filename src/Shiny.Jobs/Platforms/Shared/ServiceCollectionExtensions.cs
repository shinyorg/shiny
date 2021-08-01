using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseJobs(this IServiceCollection services, bool? clearPrevJobs = null)
        {
            if (clearPrevJobs != null)
                JobsStartup.ClearJobsBeforeRegistering = clearPrevJobs.Value;

            services.TryAddSingleton<JobsStartup>();
            services.TryAddSingleton<JobLifecycleTask>();
#if __IOS__
            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#else
            services.TryAddSingleton<IJobManager, JobManager>();
#endif
        }
    }
}
