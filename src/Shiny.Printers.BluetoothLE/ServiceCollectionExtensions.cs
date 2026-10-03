using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printers.BluetoothLE;

namespace Shiny;


public static class BlePrintersServiceCollectionExtensions
{
    /// <summary>
    /// Registers Bluetooth LE thermal printing: <see cref="IPrinterScanner"/> and <see cref="BlePrinterManager"/>.
    /// On iOS, Mac Catalyst, macOS, Android and Windows this also registers the Shiny BLE stack - calling
    /// <c>AddBluetoothLE&lt;TDelegate&gt;()</c> yourself as well is fine.
    /// </summary>
    /// <remarks>
    /// On Linux (or any plain .NET host) register <c>AddBluetoothLE()</c> from <c>Shiny.BluetoothLE.Linux</c>
    /// <b>before</b> calling this.
    /// </remarks>
    public static IServiceCollection AddBluetoothLePrinting(this IServiceCollection services)
    {
#if APPLE || ANDROID || WINDOWS
        services.AddBluetoothLE();
#else
        // The base target has no BLE implementation of its own. Say so now rather than failing later
        // with an unresolved IBleManager from somewhere deep in a scan.
        if (!services.HasService<Shiny.BluetoothLE.IBleManager>())
            throw new InvalidOperationException("No IBleManager is registered. BLE printing needs a BluetoothLE implementation - call AddBluetoothLE() from Shiny.BluetoothLE.Linux (or your host's equivalent) before AddBluetoothLePrinting().");
#endif
        services.TryAddSingleton<IPrinterScanner, BlePrinterScanner>();
        services.TryAddSingleton<BlePrinterManager>();
        return services;
    }
}
