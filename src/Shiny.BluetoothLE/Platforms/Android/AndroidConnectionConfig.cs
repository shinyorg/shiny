using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public record AndroidConnectionConfig(
    bool AutoConnect = true,
    int? RequestedMtu = null,
    GattConnectionPriority ConnectionPriority = GattConnectionPriority.Balanced
) : ConnectionConfig(
    AutoConnect
);
