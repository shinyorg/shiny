using Shiny.Jobs;

namespace Shiny.DeviceTests.Tests;


public static class JobsRegistration
{
    public static IServiceCollection AddJobsDeviceTests(this IServiceCollection services)
    {
        services.AddSingleton<JobRunLog>();
        services.AddJob<DeviceTestJob>();
        services.AddJob<FailingDeviceTestJob>();
        services.AddJob<ThrottledDeviceTestJob>();
        services.AddJob<ForegroundDeviceTestJob>(x => x.WithForeground());
        services.AddJob<CriteriaDeviceTestJob>(x => x
            .WithInternet(InternetAccess.Unmetered)
            .WithCharging()
            .WithBatteryNotLow()
        );
        services.AddDeviceTestSuite<JobTests>();
        return services;
    }
}
