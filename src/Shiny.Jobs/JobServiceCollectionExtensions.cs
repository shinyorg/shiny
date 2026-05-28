using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
#if PLATFORM
using Shiny.Jobs.Infrastructure;
#endif

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register a singleton job with optional fluent configuration.
    /// </summary>
    /// <example>
    /// <code>services.AddJob&lt;MyJob&gt;(r =&gt; r.WithIdentifier("MyJob").WithForeground());</code>
    /// </example>
    public static IServiceCollection AddJob<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJob>(this IServiceCollection services, Func<JobRegistration, JobRegistration>? configure = null)
        where TJob : class, IJob
    {
        services.AddJobs();

        var registrar = (JobRegistrar)services.Single(x => x.ImplementationInstance is JobRegistrar).ImplementationInstance!;
        registrar.Register<TJob>(configure);

        return services;
    }


    /// <summary>
    /// Registers the Shiny <see cref="IJobManager"/> and its supporting infrastructure. On
    /// platform targets (iOS/Android/Windows) this hooks the native background scheduler; on
    /// plain .NET targets it registers an in-process manager that runs jobs while the host is alive.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
#if PLATFORM
        services.AddConnectivity();
        services.AddBattery();
#endif
        if (!services.HasService<IJobManager>())
        {
            services.AddSingleton(new JobRegistrar(services));
#if PLATFORM
            services.AddSingletonAsImplementedInterfaces<JobLifecycleTask>();
            services.AddSingletonAsImplementedInterfaces<JobManager>();
#else
            services.AddSingletonAsImplementedInterfaces<JobManager>();
#endif
        }
        return services;
    }
}
