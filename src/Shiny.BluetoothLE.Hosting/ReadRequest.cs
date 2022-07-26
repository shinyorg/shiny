namespace Shiny.BluetoothLE.Hosting;


public record ReadRequest(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    int Offset
);