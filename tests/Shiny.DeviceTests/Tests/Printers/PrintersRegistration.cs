namespace Shiny.DeviceTests.Tests;


public static class PrintersRegistration
{
    public static IServiceCollection AddNetworkPrinterDeviceTests(this IServiceCollection services)
    {
        // also registers IMdnsManager when the Discovery module has not
        services.AddNetworkPrinting();
        services.AddDeviceTestSuite<NetworkPrinterTests>();
        return services;
    }
}
