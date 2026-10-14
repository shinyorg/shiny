namespace Shiny.DeviceTests.Tests;


public static class CallDirectoryRegistration
{
    public static IServiceCollection AddCallDirectoryDeviceTests(this IServiceCollection services)
    {
        // no ShinyCallDirectoryExtension in this app, so iOS exercises the store and reports no extension
        services.AddCallDirectory();
        services.AddDeviceTestSuite<CallDirectoryTests>();
        return services;
    }
}
