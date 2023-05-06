using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public record AndroidConnectionConfig(
    bool AutoConnect = true,
    GattConnectionPriority ConnectionPriority = GattConnectionPriority.Balanced
) : ConnectionConfig(
    AutoConnect
);
