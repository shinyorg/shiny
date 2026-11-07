using Shiny.BluetoothLE.AccessorySetup;

namespace Shiny.BluetoothLE;


/// <summary>Bridges accessories from the system picker to Shiny.BluetoothLE.</summary>
public static class AccessorySetupExtensions
{
    /// <summary>
    /// The <see cref="IPeripheral"/> for an accessory the user picked, ready to connect - no scan needed. Null when
    /// the accessory has no Bluetooth identity, or Bluetooth no longer knows it.
    /// </summary>
    public static IPeripheral? GetPeripheral(this IBleManager bleManager, SetupAccessory accessory)
    {
        ArgumentNullException.ThrowIfNull(bleManager);
        ArgumentNullException.ThrowIfNull(accessory);

        return accessory.PeripheralUuid == null
            ? null
            : bleManager.GetKnownPeripheral(accessory.PeripheralUuid);
    }


    /// <summary>
    /// Shows the picker for one kind of accessory - shorthand for
    /// <see cref="IAccessorySetupManager.ShowPicker"/> with a single item.
    /// </summary>
    public static Task<SetupAccessory?> ShowPicker(
        this IAccessorySetupManager manager,
        AccessoryPickerItem item,
        AccessoryPickerOptions? options = null,
        CancellationToken cancelToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(item);
        return manager.ShowPicker([item], options, cancelToken);
    }
}
