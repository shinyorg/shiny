using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE;

public record ScanConfig(
    /// <summary>
    /// Scan types - balanced & low latency are supported only on android
    /// </summary>
    BleScanType ScanType = BleScanType.Balanced,

    /// <summary>
    /// Allows the use of Scan Batching, if supported by the underlying provider
    /// Currently, this only affects Android peripherals
    /// It defaults to false to be transparent/non-breaking with existing code
    /// </summary>
    bool AndroidUseScanBatching = false, // TODO: move this to compiler flags

    /// <summary>
    /// Filters scan to peripherals that advertise specified service UUIDs
    /// iOS - you must set this to initiate a background scan
    /// </summary>
    params string[] ServiceUuids
);
