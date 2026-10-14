namespace Shiny.DeviceTests.Tests;


public static class GpsRegistration
{
    public static IServiceCollection AddGpsDeviceTests(this IServiceCollection services)
    {
        services.AddGps<TestGpsDelegate>();
        services.AddDeviceTestSuite<GpsTests>();
        return services;
    }
}
