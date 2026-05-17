using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Jobs;


/// <summary>
/// Registration record describing a job and the runtime constraints under which it may execute.
/// </summary>
/// <param name="Identifier">Unique identifier for the registration.</param>
/// <param name="JobType">The CLR type implementing <see cref="IJob"/>.</param>
/// <param name="RunOnForeground">When true, the job may run while the app is in the foreground.</param>
/// <param name="RequiredInternetAccess">The internet access required for the job to execute.</param>
/// <param name="DeviceCharging">When true, the job runs only while charging.</param>
/// <param name="BatteryNotLow">When true, the job runs only when the device battery is not low.</param>
public record JobRegistration(
    string Identifier,
    Type JobType,
    bool RunOnForeground = false,
    InternetAccess RequiredInternetAccess = InternetAccess.None,
    bool DeviceCharging = false,
    bool BatteryNotLow = false
);


/// <summary>
/// Collects job registrations during host configuration and registers them with the DI container.
/// </summary>
public sealed class JobRegistrar(IServiceCollection services)
{
    readonly Dictionary<Type, JobRegistration> registrations = new();

    /// <summary>
    /// Gets all jobs registered so far, keyed by their CLR type.
    /// </summary>
    public IReadOnlyDictionary<Type, JobRegistration> Jobs => this.registrations;


    /// <summary>
    /// Registers a job of type <typeparamref name="TJob"/> with the service collection and stores its registration.
    /// </summary>
    /// <typeparam name="TJob">The job type to register.</typeparam>
    /// <param name="registration">Optional registration metadata; defaults are used if omitted.</param>
    /// <returns>This registrar, for fluent chaining.</returns>
    public JobRegistrar Register<TJob>(JobRegistration? registration = null) where TJob : class, IJob
    {
        var reg = new JobRegistration(
            registration?.Identifier ?? typeof(TJob).FullName!,
            typeof(TJob),
            registration?.RunOnForeground ?? false,
            registration?.RequiredInternetAccess ?? InternetAccess.None,
            registration?.DeviceCharging ?? false,
            registration?.BatteryNotLow ?? false
        );
        this.registrations[typeof(TJob)] = reg;
        services.AddSingleton<TJob>();
        services.AddSingleton<IJob>(sp => sp.GetRequiredService<TJob>());
        return this;
    }
}
