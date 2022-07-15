namespace Shiny.BluetoothLE.Hosting;

public record CharacteristicSubscription(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    bool IsSubscribing
);