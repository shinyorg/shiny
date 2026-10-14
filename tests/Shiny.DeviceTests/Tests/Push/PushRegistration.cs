namespace Shiny.DeviceTests.Tests;


public static class PushRegistration
{
    public static IServiceCollection AddPushDeviceTests(this IServiceCollection services)
    {
        // no aps-environment entitlement and no google-services.json here - the suite exercises the delivery
        // path (simulated APNs payloads) and the "not supported" contract, never real registration
        services.AddPush<TestPushDelegate>();
        services.AddDeviceTestSuite<PushTests>();
        return services;
    }
}
