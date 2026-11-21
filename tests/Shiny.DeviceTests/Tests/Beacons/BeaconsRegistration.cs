namespace Shiny.DeviceTests.Tests;


public static class BeaconsRegistration
{
    public static IServiceCollection AddBeaconsDeviceTests(this IServiceCollection services)
    {
        services.AddBeaconMonitoring<TestBeaconMonitorDelegate>();
        services.AddDeviceTestSuite<BeaconsTests>();
        return services;
    }
}
