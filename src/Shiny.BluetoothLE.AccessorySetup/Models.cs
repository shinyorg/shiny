namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// What the system looks for while the picker is open. At least one of <see cref="ServiceUuid"/> and
/// <see cref="CompanyIdentifier"/> is required; the other properties narrow the match further.
/// <para>
/// On iOS every filter you use must also be declared in Info.plist - <c>NSAccessorySetupBluetoothServices</c> for
/// service UUIDs, <c>NSAccessorySetupBluetoothCompanyIdentifiers</c> for company identifiers and
/// <c>NSAccessorySetupBluetoothNames</c> for name substrings - or the picker refuses the request.
/// </para>
/// </summary>
public record AccessoryDiscoveryFilter
{
    /// <summary>A service UUID the accessory advertises (16-bit <c>180D</c> or a full 128-bit UUID).</summary>
    public string? ServiceUuid { get; init; }

    /// <summary>
    /// Service data (advertised under <see cref="ServiceUuid"/>) the advertisement must match. Requires
    /// <see cref="ServiceUuid"/>.
    /// </summary>
    public byte[]? ServiceData { get; init; }

    /// <summary>Which bits of <see cref="ServiceData"/> must match; the same length as it. Null compares every bit.</summary>
    public byte[]? ServiceDataMask { get; init; }

    /// <summary>The Bluetooth SIG company identifier in the accessory's manufacturer data.</summary>
    public ushort? CompanyIdentifier { get; init; }

    /// <summary>
    /// Manufacturer data the advertisement must start with, after the two company identifier bytes. Requires
    /// <see cref="CompanyIdentifier"/>.
    /// </summary>
    public byte[]? ManufacturerData { get; init; }

    /// <summary>Which bits of <see cref="ManufacturerData"/> must match; the same length as it. Null compares every bit.</summary>
    public byte[]? ManufacturerDataMask { get; init; }

    /// <summary>Text the advertised local name must contain (case sensitive).</summary>
    public string? NameSubstring { get; init; }

    /// <summary>
    /// How close the accessory must be. <see cref="AccessoryRange.Immediate"/> only offers accessories held next to
    /// the phone - the AirPods feel. iOS only; Android has no proximity filter and ignores it.
    /// </summary>
    public AccessoryRange Range { get; init; } = AccessoryRange.Default;

    /// <summary>
    /// iOS: the accessory requires LE pairing (bonding), so the sheet runs the pairing as part of setup
    /// (<c>ASAccessorySupportOptions.BluetoothPairingLE</c>). Android bonds on first encrypted access, as usual.
    /// </summary>
    public bool RequiresPairing { get; init; }
}


/// <summary>How close an accessory has to be for the picker to offer it.</summary>
public enum AccessoryRange
{
    /// <summary>Anything in Bluetooth range.</summary>
    Default,

    /// <summary>Only accessories right next to the phone (iOS).</summary>
    Immediate
}


/// <summary>
/// One kind of accessory the picker can offer.
/// </summary>
/// <param name="Name">The product name the sheet shows (iOS) and the accessory's display name when the platform has none.</param>
/// <param name="Filter">How to recognise the accessory.</param>
public record AccessoryPickerItem(string Name, AccessoryDiscoveryFilter Filter)
{
    /// <summary>
    /// iOS: the product image on the sheet - an image in the app bundle (<c>UIImage.FromBundle</c> name) or a file
    /// path. Use a transparent PNG. Null shows a generic Bluetooth symbol. Android's dialog has no image.
    /// </summary>
    public string? ProductImage { get; init; }

    /// <summary>iOS: let the user rename the accessory on the sheet as part of setup.</summary>
    public bool AllowRename { get; init; }
}


/// <summary>Options for <see cref="IAccessorySetupManager.ShowPicker"/>.</summary>
public record AccessoryPickerOptions
{
    /// <summary>
    /// Android: ask "pair with this device?" for the first matching device instead of listing every match. Only use
    /// it when one accessory is expected nearby - with several, the user cannot choose which one.
    /// </summary>
    public bool AndroidSingleDevice { get; init; }

    /// <summary>
    /// iOS 26+: how long the picker searches before giving up. Null keeps the system default.
    /// </summary>
    public TimeSpan? DiscoveryTimeout { get; init; }
}


/// <summary>Where an accessory is in setup.</summary>
public enum AccessoryAuthorizationState
{
    /// <summary>The user has not granted the app this accessory, or removed it.</summary>
    Unauthorized,

    /// <summary>iOS: chosen on the sheet, waiting for the app to finish its own setup.</summary>
    AwaitingAuthorization,

    /// <summary>The app may use the accessory.</summary>
    Authorized
}


/// <summary>
/// An accessory the user granted to the app through the system picker.
/// </summary>
/// <param name="Identifier">
/// Stable identifier for the grant: the accessory's Bluetooth identifier on iOS, the association id on Android 13+,
/// and the MAC address on older Android.
/// </param>
/// <param name="DisplayName">The name the user sees for it, when the platform reports one.</param>
/// <param name="PeripheralUuid">
/// The Shiny.BluetoothLE peripheral id - pass the accessory to <c>IBleManager.GetPeripheral(accessory)</c> to talk to
/// it. Null for an accessory the platform did not report a Bluetooth identity for.
/// </param>
/// <param name="State">Where it is in setup.</param>
public record SetupAccessory(
    string Identifier,
    string? DisplayName,
    string? PeripheralUuid,
    AccessoryAuthorizationState State
)
{
    /// <summary>Android: the accessory's MAC address. Null on iOS, which never exposes one.</summary>
    public string? MacAddress { get; init; }
}
