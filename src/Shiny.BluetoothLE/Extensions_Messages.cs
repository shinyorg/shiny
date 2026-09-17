using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// Writes and receives messages longer than one GATT operation, using <see cref="BleMessageFraming"/>.
/// </summary>
/// <remarks>
/// The peripheral must speak the same format - a Shiny.BluetoothLE.Hosting service with
/// <c>[RequestResponseCharacteristic(Framed = true)]</c>, or one that uses <c>NotifyMessage</c> and a
/// <see cref="BleMessageReassembler"/> itself.
/// </remarks>
public static class MessageExtensions
{
    static readonly ConditionalWeakTable<IPeripheral, ConcurrentDictionary<string, SemaphoreSlim>> writeLocks = new();


    /// <summary>
    /// Writes a message to a characteristic, split into as many writes as the peripheral's MTU needs.
    /// </summary>
    /// <param name="peripheral">The connected peripheral.</param>
    /// <param name="serviceUuid">The service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <param name="message">The complete message.</param>
    /// <param name="withResponse">Write each fragment with response. Leave on unless the peripheral only accepts commands.</param>
    /// <param name="cancellationToken">Stops sending further fragments.</param>
    /// <param name="timeoutMs">Timeout for each individual write.</param>
    /// <remarks>
    /// Messages to the same characteristic are sent one at a time, so concurrent callers cannot interleave
    /// fragments. If a write fails part way through, the peripheral discards the partial message when the
    /// next message's first fragment arrives.
    /// </remarks>
    public static async Task WriteCharacteristicMessageAsync(
        this IPeripheral peripheral,
        string serviceUuid,
        string characteristicUuid,
        byte[] message,
        bool withResponse = true,
        CancellationToken cancellationToken = default,
        int timeoutMs = 3000
    )
    {
        if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));
        if (message == null) throw new ArgumentNullException(nameof(message));

        var fragments = BleMessageFraming.Encode(message, peripheral.Mtu);
        var gate = writeLocks
            .GetValue(peripheral, static _ => new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase))
            .GetOrAdd(serviceUuid + "/" + characteristicUuid, static _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var fragment in fragments)
            {
                await peripheral
                    .WriteCharacteristicAsync(serviceUuid, characteristicUuid, fragment, withResponse, cancellationToken, timeoutMs)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
        }
    }


    /// <summary>
    /// Subscribes to a characteristic and emits each complete message the peripheral sends on it.
    /// </summary>
    /// <param name="peripheral">The connected peripheral.</param>
    /// <param name="serviceUuid">The service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <param name="useIndicationsIfAvailable">Use indications instead of notifications when both are supported.</param>
    /// <param name="maxMessageBytes">The largest message accepted; a larger one is discarded.</param>
    /// <returns>
    /// A cold observable. Each subscription subscribes to the characteristic and gets its own reassembler.
    /// A message with a dropped or reordered fragment is discarded rather than emitted, and the stream carries
    /// on with the next one.
    /// </returns>
    public static IObservable<byte[]> NotifyCharacteristicMessages(
        this IPeripheral peripheral,
        string serviceUuid,
        string characteristicUuid,
        bool useIndicationsIfAvailable = true,
        int maxMessageBytes = BleMessageFraming.DefaultMaxMessageBytes
    )
    {
        if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));

        return Observable.Defer(() =>
        {
            var reassembler = new BleMessageReassembler(maxMessageBytes);

            return peripheral
                .NotifyCharacteristic(serviceUuid, characteristicUuid, useIndicationsIfAvailable)
                .Where(x => x.Event == BleCharacteristicEvent.Notification)
                .Select(x =>
                {
                    lock (reassembler)
                        return reassembler.Push(x.Data ?? Array.Empty<byte>(), out var message) == BleMessageFrameResult.Complete
                            ? message
                            : null;
                })
                .Where(x => x != null)
                .Select(x => x!);
        });
    }
}
