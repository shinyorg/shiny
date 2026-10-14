namespace Shiny.DeviceTests.Tests;


public static class CoreRegistration
{
    public static IServiceCollection AddCoreDeviceTests(this IServiceCollection services)
    {
        services.AddConnectivity();
        services.AddBattery();
        services.AddDeviceTestSuite<CoreTests>();
        return services;
    }
}
