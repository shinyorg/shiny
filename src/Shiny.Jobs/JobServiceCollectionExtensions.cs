#if PLATFORM
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
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

            services.AddSingleton<JobLifecycleTask>();
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<JobLifecycleTask>());
#if ANDROID
            services.AddSingleton<IAndroidLifecycle.IApplicationLifecycle>(sp => sp.GetRequiredService<JobLifecycleTask>());
#elif IOS || MACCATALYST
            services.AddSingleton<IIosLifecycle.IApplicationLifecycle>(sp => sp.GetRequiredService<JobLifecycleTask>());
#elif MACOS
            services.AddSingleton<IMacLifecycle.IApplicationLifecycle>(sp => sp.GetRequiredService<JobLifecycleTask>());
#endif

            services.AddSingleton<JobManager>();
            services.AddSingleton<IJobManager>(sp => sp.GetRequiredService<JobManager>());
#if IOS || MACCATALYST
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<JobManager>());
#endif
        }

        var registrar = (JobRegistrar)services.Single(x => x.ImplementationInstance is JobRegistrar).ImplementationInstance!;
        registrar.Register<TJob>(registration);

        return services;
    }
}
#endif
