#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a job on the job manager
    /// </summary>
    /// <param name="services">The service collection to register with</param>
    /// <param name="jobInfo">The job info to register</param>
    /// <param name="clearJobQueueFirst">If set to true, before registering all new jobs during startup, an command will be issued to clear out any previous jobs - this is useful during application upgrades or if you aren't manually registering jobs</param>
    public static IServiceCollection AddJob(this IServiceCollection services, JobInfo jobInfo, bool? clearJobQueueFirst = null)
    {
        JobsStartup.AddJob(jobInfo);
        return services.AddJobs(clearJobQueueFirst);
    }


    /// <summary>
    /// Registers a job on the job manager
    /// </summary>
    /// <param name="services"></param>
    /// <param name="jobType"></param>
    /// <param name="identifier"></param>
    public static IServiceCollection AddJob(
        this IServiceCollection services,
        Type jobType,
        string? identifier = null,
        InternetAccess requiredNetwork = InternetAccess.None,
        bool runInForeground = false,
        bool? clearJobQueueFirst = null,
        params (string Key, object value)[] parameters
    )
        => services.AddJob(new JobInfo(jobType, identifier)
        {
            RequiredInternetAccess = requiredNetwork,
            RunOnForeground = runInForeground,
            Repeat = true,
            //Parameters = parameters?.ToDictionary() // TODO
        }, clearJobQueueFirst);


    public static IServiceCollection AddJobs(this IServiceCollection services, bool? clearPrevJobs = null)
    {
        if (clearPrevJobs != null)
            JobsStartup.ClearJobsBeforeRegistering = clearPrevJobs.Value;

        services.AddRepository<JobInfoStoreConverter, JobInfo>();
        services.AddShinyService<JobsStartup>();
        services.TryAddSingleton<IJobManager, JobManager>();

        services.AddBattery();
        services.AddConnectivity();
        services.AddShinyService<JobLifecycleTask>();
        return services;
    }
}
#endif