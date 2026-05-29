using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// Fluent extension methods for configuring a <see cref="JobRegistration"/>.
/// </summary>
public static class JobRegistrationExtensions
{
    /// <summary>Overrides the registration identifier.</summary>
    public static JobRegistration WithIdentifier(this JobRegistration reg, string identifier)
        => reg with { Identifier = identifier };

    /// <summary>Allows the job to run while the app is in the foreground.</summary>
    public static JobRegistration WithForeground(this JobRegistration reg, bool value = true)
        => reg with { RunOnForeground = value };

    /// <summary>Sets the internet access required for the job to execute.</summary>
    public static JobRegistration WithInternet(this JobRegistration reg, InternetAccess access)
        => reg with { RequiredInternetAccess = access };

    /// <summary>Restricts the job to run only while the device is charging.</summary>
    public static JobRegistration WithCharging(this JobRegistration reg, bool value = true)
        => reg with { DeviceCharging = value };

    /// <summary>Restricts the job to run only when the device battery is not low.</summary>
    public static JobRegistration WithBatteryNotLow(this JobRegistration reg, bool value = true)
        => reg with { BatteryNotLow = value };
}


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
    /// <param name="configure">Optional fluent configuration; the default registration uses <c>typeof(TJob).FullName</c> as the identifier.</param>
    /// <returns>This registrar, for fluent chaining.</returns>
    public JobRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJob>(Func<JobRegistration, JobRegistration>? configure = null) where TJob : class, IJob
    {
        var reg = new JobRegistration(typeof(TJob).FullName!, typeof(TJob));
        if (configure != null)
            reg = configure(reg);

        this.registrations[typeof(TJob)] = reg;
        services.AddScopedAsImplementedInterfaces<TJob>();
        return this;
    }
}
