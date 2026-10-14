namespace Shiny.DeviceTests.Tests;


public static class NotificationsRegistration
{
    public static IServiceCollection AddNotificationDeviceTests(this IServiceCollection services)
    {
#if IOS || MACCATALYST
        // Provisional authorization is granted without a prompt. Simctl can't pre-grant notifications and a
        // headless simulator has no window for DevFlow to tap "Allow" in, so a normal request would hang the run.
        // Registered before AddNotifications, which only TryAdds its default configuration.
        services.AddSingleton(new Shiny.Notifications.IosConfiguration(
            UserNotifications.UNAuthorizationOptions.Alert |
            UserNotifications.UNAuthorizationOptions.Badge |
            UserNotifications.UNAuthorizationOptions.Sound |
            UserNotifications.UNAuthorizationOptions.Provisional
        ));
#endif
        services.AddNotifications<TestNotificationDelegate>();
        services.AddDeviceTestSuite<NotificationTests>();
        return services;
    }
}
