using Microsoft.Extensions.DependencyInjection;
using Shiny.Bluetooth;

namespace Shiny;


// Platform targets only. The plain .NET target is what Shiny.Bluetooth.Linux builds against, and it
// declares AddLinuxBluetooth itself - the same split as Shiny.BluetoothLE / Shiny.BluetoothLE.Linux.
#if PLATFORM
public static class BluetoothServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBluetoothManager"/> for classic Bluetooth (BR/EDR) - RFCOMM client and
    /// server, discovery, pairing and discoverability.
    /// </summary>
    /// <remarks>
    /// <para>Android: manifest needs <c>BLUETOOTH_SCAN</c> and <c>BLUETOOTH_CONNECT</c> (API 31+, plus
    /// <c>BLUETOOTH_ADVERTISE</c> for <see cref="IBluetoothManager.RequestDiscoverable"/>), and
    /// <c>BLUETOOTH</c>, <c>BLUETOOTH_ADMIN</c> and <c>ACCESS_FINE_LOCATION</c> with
    /// <c>android:maxSdkVersion="30"</c> for older devices.</para>
    /// <para>Windows: needs the <c>bluetooth</c> device capability (and <c>radios</c>) when packaged.</para>
    /// <para>macOS: needs <c>NSBluetoothAlwaysUsageDescription</c> in Info.plist, and
    /// <c>com.apple.security.device.bluetooth</c> when sandboxed.</para>
    /// <para>Linux: this method does not exist on the plain <c>net10.0</c> target. Reference
    /// <c>Shiny.Bluetooth.Linux</c> and call <c>AddLinuxBluetooth</c>.</para>
    /// </remarks>
    public static IServiceCollection AddBluetooth(this IServiceCollection services)
    {
        if (!services.HasService<IBluetoothManager>())
        {
#if ANDROID
            services.AddSingleton<IBluetoothManager, AndroidBluetoothManager>();
#elif WINDOWS
            services.AddSingleton<IBluetoothManager, WindowsBluetoothManager>();
#elif MACOS
            services.AddSingleton<IBluetoothManager, MacBluetoothManager>();
#endif
        }
        return services;
    }
}
#endif
