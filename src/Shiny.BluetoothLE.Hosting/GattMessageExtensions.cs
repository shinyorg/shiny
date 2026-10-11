using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Sends and receives messages longer than one GATT operation, using <see cref="BleMessageFraming"/>.
/// </summary>
/// <remarks>
/// The central side of the same format is <c>WriteCharacteristicMessageAsync</c> and
/// <c>NotifyCharacteristicMessages</c> in Shiny.BluetoothLE. To receive framed writes, feed each fragment to a
/// <see cref="BleMessageReassembler"/> you keep per central.
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
}
