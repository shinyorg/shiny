#if !PLATFORM
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny;


public class JobBuilder
{
    public string? Id { get; set; }
    public bool Foreground { get; set; }
    public InternetAccess Network { get; set; } = InternetAccess.None;
    public bool Charging { get; set; }
    public bool BatteryOk { get; set; }
}


public static class ServiceCollectionExtensions
{
    static readonly List<JobRegistration> registeredJobs = new();


    /// <summary>
    /// Register a job with optional fluent configuration for plain .NET targets.
    /// </summary>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services, Action<JobBuilder>? configure = null)
        where TJob : class, IJob
    {
        var builder = new JobBuilder();
        configure?.Invoke(builder);
        var reg = new JobRegistration(
            builder.Id ?? typeof(TJob).FullName!,
            typeof(TJob),
            builder.Foreground,
            builder.Network,
            builder.Charging,
            builder.BatteryOk
        );
        registeredJobs.Add(reg);
        services.AddShinyService<TJob>();
        return services.AddJobs();
    }


    /// <summary>
    /// Registers the in-process Shiny JobManager for plain .NET targets.
    /// Jobs only run while the host process is alive.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        if (!services.HasService<IJobManager>())
            services.AddShinyService<JobManager>();

        return services;
    }


    internal static IReadOnlyList<JobRegistration> GetRegisteredJobs() => registeredJobs;
}
#endif
