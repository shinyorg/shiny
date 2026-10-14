namespace Shiny.DeviceTests.Tests;


public static class ContactsRegistration
{
    public static IServiceCollection AddContactsDeviceTests(this IServiceCollection services)
    {
        services.AddContactStore();
        services.AddDeviceTestSuite<ContactsTests>();
        return services;
    }
}
