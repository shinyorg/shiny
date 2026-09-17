using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Sends and receives messages longer than one GATT operation, using <see cref="BleMessageFraming"/>.
/// </summary>
/// <remarks>
/// The central side of the same format is <c>WriteCharacteristicMessageAsync</c> and
/// <c>NotifyCharacteristicMessages</c> in Shiny.BluetoothLE. For a generated service, set
/// <c>Framed = true</c> on <see cref="RequestResponseCharacteristicAttribute"/> instead of calling these.
/// </remarks>
public static class GattMessageExtensions
{
    /// <summary>
    /// Notifies one central of a message, split into as many notifications as its MTU needs.
    /// </summary>
    /// <param name="characteristic">A characteristic the central is subscribed to.</param>
    /// <param name="message">The complete message.</param>
    /// <param name="central">The central to send it to. Fragments are sized for this central's MTU.</param>
    /// <param name="cancellationToken">Stops sending further fragments.</param>
    /// <remarks>
    /// The fragments go out one after another, each waiting for the platform to accept the last. Do not send
    /// two messages to the same central on the same characteristic concurrently - their fragments would
    /// interleave and the central would discard both.
    /// </remarks>
    public static async Task NotifyMessage(
        this IGattCharacteristic characteristic,
        byte[] message,
        IPeripheral central,
        CancellationToken cancellationToken = default
    )
    {
        if (characteristic == null) throw new ArgumentNullException(nameof(characteristic));
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (central == null) throw new ArgumentNullException(nameof(central));

        foreach (var fragment in BleMessageFraming.Encode(message, central.Mtu))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await characteristic.Notify(fragment, cancellationToken, central).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Gets the reassembler that collects one characteristic's framed writes from the central this context
    /// belongs to, creating it on first use.
    /// </summary>
    /// <param name="context">The central's context.</param>
    /// <param name="characteristicUuid">The characteristic being written.</param>
    /// <param name="maxMessageBytes">The size cap applied when the reassembler is created.</param>
    /// <remarks>
    /// Held on the context, so it lives exactly as long as the central's connection and is never shared with
    /// another central. Feed it from the characteristic's write handler, one fragment at a time.
    /// </remarks>
    public static BleMessageReassembler GetMessageReassembler(
        this BleServiceContext context,
        string characteristicUuid,
        int maxMessageBytes = BleMessageFraming.DefaultMaxMessageBytes
    )
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (characteristicUuid == null) throw new ArgumentNullException(nameof(characteristicUuid));

        var key = "shiny:ble:frames:" + characteristicUuid.ToUpperInvariant();
        var items = context.Items;

        lock (items)
        {
            if (items.TryGetValue(key, out var existing) && existing is BleMessageReassembler reassembler)
                return reassembler;

            var created = new BleMessageReassembler(maxMessageBytes);
            items[key] = created;
            return created;
        }
    }
}
