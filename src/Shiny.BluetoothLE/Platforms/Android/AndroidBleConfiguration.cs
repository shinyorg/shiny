namespace Shiny.BluetoothLE;


public record AndroidBleConfiguration(
    /// <summary>
    /// Allows you to disable the internal sync queue
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool UseInternalSyncQueue = true
);