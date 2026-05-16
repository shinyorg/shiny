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
    /// Register a singleton job with optional fluent configuration
    /// </summary>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services, JobRegistration? registration = null)
        where TJob : class, IJob
    {
        services.AddConnectivity();
        services.AddBattery();

        if (!services.HasService<IJobManager>())
        {
            services.AddSingleton(new JobRegistrar(services));
            services.AddShinyService<JobLifecycleTask>();
            services.AddShinyService<JobManager>();
        }

        var registrar = (JobRegistrar)services.Single(x => x.ImplementationInstance is JobRegistrar).ImplementationInstance!;
        registrar.Register<TJob>(registration);
            
        return services;
    }
}
#endif
