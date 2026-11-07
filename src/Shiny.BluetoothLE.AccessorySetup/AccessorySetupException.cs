namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>Why an accessory setup operation failed.</summary>
public enum AccessorySetupErrorCode
{
    /// <summary>This OS version or device has no accessory setup API (see <see cref="IAccessorySetupManager.IsSupported"/>).</summary>
    NotSupported,

    /// <summary>A picker is already showing.</summary>
    PickerAlreadyActive,

    /// <summary>The user or a device management profile has restricted accessory setup.</summary>
    Restricted,

    /// <summary>
    /// The request is invalid - a bad <see cref="AccessoryDiscoveryFilter"/>, or (iOS) a filter that Info.plist does
    /// not declare, or (Android) a permission missing from the manifest.
    /// </summary>
    InvalidRequest,

    /// <summary>The accessory is not (or no longer) granted to the app.</summary>
    NotAssociated,

    /// <summary>The platform failed the operation; the inner exception or message has its reason.</summary>
    Failed
}


/// <summary>An accessory setup operation failed; <see cref="Code"/> says why.</summary>
public class AccessorySetupException(AccessorySetupErrorCode code, string message, Exception? inner = null) : Exception(message, inner)
{
    /// <summary>Why it failed.</summary>
    public AccessorySetupErrorCode Code { get; } = code;
}
