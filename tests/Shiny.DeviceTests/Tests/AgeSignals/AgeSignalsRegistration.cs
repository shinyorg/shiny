namespace Shiny.DeviceTests.Tests;


public static class AgeSignalsRegistration
{
    public static IServiceCollection AddAgeSignalsDeviceTests(this IServiceCollection services)
    {
        services.AddAgeSignals();
        services.AddDeviceTestSuite<AgeSignalsTests>();
        return services;
    }
}
