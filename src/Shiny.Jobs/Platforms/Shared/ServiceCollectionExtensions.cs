#if PLATFORM
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
    public static IServiceCollection AddJob(this IServiceCollection services, JobInfo jobInfo)
    {
        JobLifecycleTask.AddJob(jobInfo);
        return services.AddJobs();
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
        params (string Key, string Value)[] parameters
    )
        => services.AddJob(new JobInfo(
            identifier ?? jobType.FullName,
            jobType,
            runInForeground,
            Parameters: parameters?.ToDictionary(x => x.Key, x => x.Value),
            RequiredInternetAccess: requiredNetwork
        ));

    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddConnectivity();
        services.AddBattery();
        
        if (!services.HasService<IJobManager>())
        {
            services.AddDefaultRepository();
            services.AddShinyService<JobLifecycleTask>();
            services.AddShinyService<JobManager>();
        }

        return services;
    }
}
#endif