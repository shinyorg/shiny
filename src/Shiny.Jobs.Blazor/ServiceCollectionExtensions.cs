using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Blazor;
using Shiny.Jobs.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddBattery();
        services.AddConnectivity();
        services.AddRepository<JobInfoStoreConverter, JobInfo>();
        //services.AddShinyService<JobsStartup>();
        //services.AddShinyService<JobLifecycleTask>();
        services.AddShinyService<JobManager>();

        return services;
    }
}

