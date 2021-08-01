using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {

        public static void UseJobs(this IServiceCollection services, bool clearPrevJobs = false, TimeSpan? foregroundJobTimer = null)
        {
            services.TryAddSingleton<JobsStartup>();
#if __IOS__
            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#else
            services.TryAddSingleton<IJobManager, JobManager>();
#endif
            if (!JobsStartup.ClearJobsBeforeRegistering)
                JobsStartup.ClearJobsBeforeRegistering = clearPrevJobs;

            if (foregroundJobTimer != null)
            {
                if (JobLifecycleTask.Interval == null || JobLifecycleTask.Interval > foregroundJobTimer.Value)
                    JobLifecycleTask.Interval = foregroundJobTimer.Value;

                services.TryAddSingleton<JobLifecycleTask>();
            }
        }
    }
}
