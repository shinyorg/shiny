namespace Shiny.BluetoothLE;


public record AndroidBleConfiguration(
    /// <summary>
    /// Allows you to disable the internal sync queue
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool UseInternalSyncQueue = true,

    /// <summary>
    /// Legacy support
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool InvokeCallsOnMainThread = false,

    /// <summary>
    /// Enable this if you are experiencing cache issues (connect, disconnect, connect, get service android issue)
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool FlushServicesBetweenConnections = false
);