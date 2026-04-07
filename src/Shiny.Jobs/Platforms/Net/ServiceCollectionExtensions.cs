using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a job on the in-process Shiny JobManager for plain .NET targets.
    /// </summary>
    public static IServiceCollection AddJob(this IServiceCollection services, JobInfo jobInfo)
    {
        JobManager.AddJob(jobInfo);
        return services.AddJobs();
    }


    /// <summary>
    /// Registers a job on the in-process Shiny JobManager for plain .NET targets.
    /// </summary>
    public static IServiceCollection AddJob(
        this IServiceCollection services,
        Type jobType,
        string? identifier = null,
        InternetAccess requiredNetwork = InternetAccess.None,
        bool runInForeground = false,
        params (string Key, string Value)[] parameters
    )
        => services.AddJob(new JobInfo(
            identifier ?? jobType.FullName!,
            jobType,
            runInForeground,
            Parameters: parameters?.ToDictionary(x => x.Key, x => x.Value),
            RequiredInternetAccess: requiredNetwork
        ));


    /// <summary>
    /// Registers the in-process Shiny JobManager for plain .NET targets.
    /// Jobs only run while the host process is alive — there is no OS-level
    /// scheduler. You must register an <see cref="Shiny.Net.IConnectivity"/>
    /// and <see cref="Shiny.Power.IBattery"/> implementation (e.g. via
    /// Shiny.Support.DeviceMonitoring.Linux) before resolving the job manager.
    /// A default JSON filesystem repository is registered automatically.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        if (!services.HasService<IJobManager>())
        {
            services.AddDefaultRepository();
            services.AddShinyService<JobManager>();
        }
        return services;
    }
}
