namespace Shiny.BluetoothLE;


public record AndroidBleConfiguration(
    /// <summary>
    /// Allows you to disable the internal sync queue
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool AndroidUseInternalSyncQueue = true,

    /// <summary>
    /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
    /// DO NOT CHANGE this if you don't know what this is!
    /// </summary>
    bool AndroidShouldInvokeOnMainThread = false
);