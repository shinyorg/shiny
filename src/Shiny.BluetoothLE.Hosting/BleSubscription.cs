namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// A central subscribing to, or unsubscribing from, a notify characteristic.
/// </summary>
/// <param name="Characteristic">The characteristic whose subscription state changed.</param>
/// <param name="Peripheral">The central that toggled its subscription.</param>
/// <param name="IsSubscribing">True when subscribing, false when unsubscribing.</param>
/// <remarks>
/// Thin wrapper over <see cref="CharacteristicSubscription"/> that exists so notify hooks have a
/// stable shape independent of the hosting library's record. Declare either type as a parameter.
/// </remarks>
public record BleSubscription(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    bool IsSubscribing
)
{
    /// <summary>
    /// Creates an instance from the hosting library's own subscription record.
    /// </summary>
    /// <param name="subscription">The source subscription.</param>
    /// <returns>The wrapped subscription.</returns>
    public static BleSubscription From(CharacteristicSubscription subscription)
        => new(subscription.Characteristic, subscription.Peripheral, subscription.IsSubscribing);
}
