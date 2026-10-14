namespace Shiny.DeviceTests.Tests;


public static class CalendarRegistration
{
    public static IServiceCollection AddCalendarDeviceTests(this IServiceCollection services)
    {
        services.AddCalendarStore();
        services.AddDeviceTestSuite<CalendarTests>();
        return services;
    }
}
