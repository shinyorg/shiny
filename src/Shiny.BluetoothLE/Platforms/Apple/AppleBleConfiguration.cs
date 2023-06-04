using CoreFoundation;

namespace Shiny.BluetoothLE;


public record AppleBleConfiguration(
    /// <summary>
    /// This will display an alert dialog when the user powers off their bluetooth adapter
    /// </summary>
    bool ShowPowerAlert = false,


    /// <summary>
    /// CBCentralInitOptions restoration key for background restoration
    /// </summary>
    string? RestoreIdentifier = null,

    /// <summary>
    /// Dispatch queue for CBCentralManager to use - leave null if you do not know what this does
    /// </summary>
    DispatchQueue? DispatchQueue = null
);
