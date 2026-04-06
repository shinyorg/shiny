using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Jobs.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJob(this IServiceCollection services, JobInfo jobInfo)
    {
        JobManager.AddJob(jobInfo);
        return services.AddJobs();
    }


    public static IServiceCollection AddJob(
        this IServiceCollection services,
        Type jobType,
        string? identifier = null,
        Shiny.Jobs.InternetAccess requiredNetwork = Shiny.Jobs.InternetAccess.None,
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


    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        if (!services.HasService<IJobManager>())
        {
            services.AddBattery();
            services.AddConnectivity();
            services.AddLocalStorageRepository();
            services.AddShinyService<JobManager>();
        }

        return services;
    }
}
