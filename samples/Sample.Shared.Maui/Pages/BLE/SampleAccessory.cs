namespace Sample.Shared.Maui.Pages.BLE;

/// <summary>
/// The pretend accessory the Accessory Setup page looks for and the Accessory Advertiser page hosts - run the
/// advertiser on a second device (another phone, a Mac, a Windows PC) and pick it from the first.
/// <para>
/// iOS: the service UUID and the name are declared in Sample.Maui's Info.plist
/// (NSAccessorySetupBluetoothServices / NSAccessorySetupBluetoothNames) - AccessorySetupKit refuses any filter that
/// is not.
/// </para>
/// </summary>
public static class SampleAccessory
{
    public const string ServiceUuid = "A495FF40-C5B1-4B44-B512-1370F02D74DE";
    public const string InfoCharacteristicUuid = "A495FF41-C5B1-4B44-B512-1370F02D74DE";
    public const string PingCharacteristicUuid = "A495FF42-C5B1-4B44-B512-1370F02D74DE";

    /// <summary>The advertised local name, and the name substring declared in Info.plist.</summary>
    public const string LocalName = "Shiny";

    /// <summary>Event store category both pages and the delegate log under.</summary>
    public const string EventCategory = "AccessorySetup";
}
