using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Jobs;


public record JobRegistration(
    string Identifier,
    Type JobType,
    bool RunOnForeground = false,
    InternetAccess RequiredInternetAccess = InternetAccess.None,
    bool DeviceCharging = false,
    bool BatteryNotLow = false
);


public sealed class JobRegistrar(IServiceCollection services)
{
    readonly Dictionary<Type, JobRegistration> registrations = new();
    public IReadOnlyDictionary<Type, JobRegistration> Jobs => this.registrations;


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
        services.AddShinyService<TJob>();
        return this;
    }
}
