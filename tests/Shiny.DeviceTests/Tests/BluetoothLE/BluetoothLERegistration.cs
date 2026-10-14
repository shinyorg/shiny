namespace Shiny.DeviceTests.Tests;


public static class BluetoothLERegistration
{
    public static IServiceCollection AddBluetoothLEDeviceTests(this IServiceCollection services)
    {
        services.AddBluetoothLE<TestBleDelegate>();
        services.AddBluetoothLeHosting();

        // the two halves of the loop - singletons so a peer command and the suites share one connection / one hosted profile
        services.AddSingleton<BleTestHost>();
        services.AddSingleton<BleTestCentral>();

        services.AddDeviceTestSuite<BleLocalTests>();
        services.AddDeviceTestSuite<BleCentralTests>();
        services.AddDeviceTestSuite<BleHostingTests>();

        services.AddPeerCommand<BleHostStartCommand>();
        services.AddPeerCommand<BleHostStopCommand>();
        services.AddPeerCommand<BleHostStatsCommand>();
        services.AddPeerCommand<BleCentralRunCommand>();
        services.AddPeerCommand<BleCentralDisconnectCommand>();
        return services;
    }
}
