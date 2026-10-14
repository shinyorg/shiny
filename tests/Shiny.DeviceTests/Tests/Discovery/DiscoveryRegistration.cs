namespace Shiny.DeviceTests.Tests;


public static class DiscoveryRegistration
{
    public static IServiceCollection AddDiscoveryDeviceTests(this IServiceCollection services)
    {
        services.AddMdns();
        services.AddSsdp();
        services.AddWsDiscovery();
        services.AddDeviceTestSuite<DiscoveryTests>();
        return services;
    }
}
