namespace Shiny.DeviceTests.Tests;


public static class HttpTransfersRegistration
{
    public static IServiceCollection AddHttpTransferDeviceTests(this IServiceCollection services)
    {
        services.AddHttpTransfers<TestHttpTransferDelegate>();
        services.AddDeviceTestSuite<HttpTransferTests>();
        return services;
    }
}
