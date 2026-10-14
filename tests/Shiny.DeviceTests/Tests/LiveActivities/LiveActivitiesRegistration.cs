namespace Shiny.DeviceTests.Tests;


public static class LiveActivitiesRegistration
{
    public static IServiceCollection AddLiveActivityDeviceTests(this IServiceCollection services)
    {
        // no widget extension is built (that needs provisioning) - ActivityKit still runs the activity, it just
        // has nothing to render. Mac Catalyst resolves the net10.0 no-op manager.
        services.AddLiveActivities<TestLiveActivityDelegate>();
        services.AddDeviceTestSuite<LiveActivityTests>();
        return services;
    }
}
