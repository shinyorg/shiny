namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// The system's own accessory pairing sheet - AccessorySetupKit on iOS 18+, CompanionDeviceManager on Android. The
/// user picks the accessory in system UI and that choice is the permission: no MFi, no location prompt and, on iOS,
/// no general Bluetooth prompt.
/// <para>
/// Picked accessories are ordinary Shiny.BluetoothLE peripherals - <c>bleManager.GetPeripheral(accessory)</c> - so
/// connecting, GATT and notifications go through <c>IPeripheral</c> as usual.
/// </para>
/// </summary>
public interface IAccessorySetupManager
{
    /// <summary>
    /// False before iOS 18, and on Android devices without the <c>companion_device_setup</c> feature. Everything else
    /// on this interface throws <see cref="AccessorySetupException"/> (<see cref="AccessorySetupErrorCode.NotSupported"/>)
    /// when this is false, except <see cref="GetAccessories"/>, which returns nothing.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Android 12+: <see cref="StartObservingPresence"/> works. Always false on iOS.
    /// </summary>
    bool IsPresenceSupported { get; }

    /// <summary>
    /// Accessories the user has granted this app.
    /// </summary>
    Task<IReadOnlyList<SetupAccessory>> GetAccessories(CancellationToken cancelToken = default);

    /// <summary>
    /// Shows the system picker for accessories matching any of <paramref name="items"/> and waits for the user.
    /// </summary>
    /// <returns>The accessory the user set up, or null if they dismissed the picker or nothing was found.</returns>
    /// <exception cref="AccessorySetupException">
    /// The picker could not be shown - another one is open, the request or Info.plist is invalid, or the platform
    /// refused.
    /// </exception>
    Task<SetupAccessory?> ShowPicker(
        IReadOnlyList<AccessoryPickerItem> items,
        AccessoryPickerOptions? options = null,
        CancellationToken cancelToken = default
    );

    /// <summary>
    /// Removes the app's grant for an accessory (iOS shows its own confirmation). The app loses access to it until
    /// it is picked again.
    /// </summary>
    Task Remove(SetupAccessory accessory, CancellationToken cancelToken = default);

    /// <summary>
    /// Android 12+: wake the app (through <see cref="IAccessorySetupDelegate.OnPresenceChanged"/>) whenever the
    /// accessory comes into or leaves range, even when the app is not running. Needs
    /// <c>android.permission.REQUEST_OBSERVE_COMPANION_DEVICE_PRESENCE</c> in the manifest.
    /// </summary>
    Task StartObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default);

    /// <summary>Stops <see cref="StartObservingPresence"/> for an accessory.</summary>
    Task StopObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default);
}
