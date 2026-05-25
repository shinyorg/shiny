#if !PLATFORM
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    static readonly List<JobRegistration> registeredJobs = new();


    /// <summary>
    /// Register a job with optional fluent configuration for plain .NET targets.
    /// </summary>
    /// <example>
    /// <code>services.AddJob&lt;MyJob&gt;(r =&gt; r.WithIdentifier("MyJob").WithForeground());</code>
    /// </example>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services, Func<JobRegistration, JobRegistration>? configure = null)
        where TJob : class, IJob
    {
        var reg = new JobRegistration(typeof(TJob).FullName!, typeof(TJob));
        if (configure != null)
            reg = configure(reg);

        registeredJobs.Add(reg);
        services.AddSingleton<TJob>();
        services.AddSingleton<IJob>(sp => sp.GetRequiredService<TJob>());
        return services.AddJobs();
    }


    /// <summary>
    /// Registers the in-process Shiny JobManager for plain .NET targets.
    /// Jobs only run while the host process is alive.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        if (!services.HasService<IJobManager>())
        {
            services.AddSingleton<JobManager>();
            services.AddSingleton<IJobManager>(sp => sp.GetRequiredService<JobManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<JobManager>());
        }

        return services;
    }


    internal static IReadOnlyList<JobRegistration> GetRegisteredJobs() => registeredJobs;
}
#endif
