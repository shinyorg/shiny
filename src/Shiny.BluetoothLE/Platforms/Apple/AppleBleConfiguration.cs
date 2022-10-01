namespace Shiny.BluetoothLE;


public record AppleBleConfiguration(
    /// <summary>
    /// This will display an alert dialog when the user powers off their bluetooth adapter
    /// </summary>
    bool ShowPowerAlert = false,


    /// <summary>
    /// CBCentralInitOptions restoration key for background restoration
    /// </summary>
    string? RestoreIdentifier = null
);
