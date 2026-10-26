namespace Shiny.DeviceTests.Tests;


public static class DeviceTestModules
{
    // each module's Add*DeviceTests lives next to its suite in Tests/<Module>/
    public static IServiceCollection AddDeviceTestModules(this IServiceCollection services)
    {
        services.AddCoreDeviceTests();
        services.AddBluetoothLEDeviceTests();
        services.AddCalendarDeviceTests();
        services.AddContactsDeviceTests();
        services.AddDiscoveryDeviceTests();
        services.AddHttpTransferDeviceTests();
        services.AddDataSyncDeviceTests();
        services.AddNetworkPrinterDeviceTests();
        services.AddGpsDeviceTests();
        services.AddGeofencingDeviceTests();
        services.AddNotificationDeviceTests();
        services.AddPushDeviceTests();
        services.AddJobsDeviceTests();
        services.AddLiveActivityDeviceTests();
        services.AddCallDirectoryDeviceTests();
        services.AddAgeSignalsDeviceTests();
        return services;
    }
}
