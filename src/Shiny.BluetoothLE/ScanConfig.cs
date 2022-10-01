namespace Shiny.BluetoothLE;


public record ScanConfig(
    /// <summary>
    /// Filters scan to peripherals that advertise specified service UUIDs
    /// iOS - you must set this to initiate a background scan
    /// </summary>
    params string[] ServiceUuids
);
