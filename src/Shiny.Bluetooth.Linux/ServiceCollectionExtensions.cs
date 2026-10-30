using Microsoft.Extensions.DependencyInjection;
using Shiny.Bluetooth;

namespace Shiny;


public static class LinuxBluetoothServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBluetoothManager"/> for Linux - classic Bluetooth through BlueZ over D-Bus.
    /// </summary>
    /// <remarks>
    /// <para>Needs a running <c>bluetoothd</c> and access to the system bus; the default D-Bus policy
    /// lets members of the <c>bluetooth</c> group (or root) talk to BlueZ.</para>
    /// <para>Pairing with a device that asks for a PIN or a confirmation needs a BlueZ agent to answer -
    /// a desktop session has one, a headless device needs <c>bluetoothctl</c>'s or its own. Without one,
    /// <see cref="IBluetoothDevice.Pair"/> returns false.</para>
    /// </remarks>
    public static IServiceCollection AddLinuxBluetooth(this IServiceCollection services)
    {
        if (!services.HasService<IBluetoothManager>())
            services.AddSingleton<IBluetoothManager, LinuxBluetoothManager>();

        return services;
    }
}
