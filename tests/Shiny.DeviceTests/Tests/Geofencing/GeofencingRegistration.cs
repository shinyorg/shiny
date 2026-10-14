namespace Shiny.DeviceTests.Tests;


public static class GeofencingRegistration
{
    public static IServiceCollection AddGeofencingDeviceTests(this IServiceCollection services)
    {
        services.AddGeofencing<TestGeofenceDelegate>();
        services.AddDeviceTestSuite<GeofencingTests>();
        return services;
    }
}
