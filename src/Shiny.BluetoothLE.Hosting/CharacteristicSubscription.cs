namespace Shiny.BluetoothLE.Hosting;

/// <summary>
/// Notification subscription event raised when a central subscribes to or unsubscribes from a hosted characteristic.
/// </summary>
/// <param name="Characteristic">The characteristic whose subscription state changed.</param>
/// <param name="Peripheral">The central that toggled its subscription.</param>
/// <param name="IsSubscribing">True when the central is subscribing; false when it is unsubscribing.</param>
public record CharacteristicSubscription(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    bool IsSubscribing
);
