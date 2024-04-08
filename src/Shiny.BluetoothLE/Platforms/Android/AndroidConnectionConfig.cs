namespace Shiny.BluetoothLE;


public record AndroidConnectionConfig(
    bool AutoConnect = true,
    Android.Bluetooth.GattConnectionPriority ConnectionPriority = Android.Bluetooth.GattConnectionPriority.Balanced
) : ConnectionConfig(
    AutoConnect
);
