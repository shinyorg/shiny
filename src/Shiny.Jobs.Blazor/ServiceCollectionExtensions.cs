using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Jobs.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddBattery();
        services.AddConnectivity();
        //services.AddRepository
        services.TryAddSingleton<IJobManager, JobManager>();

        return services;
    }
}

