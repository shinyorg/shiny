using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Async-enumerable conveniences over <see cref="L2CapChannel"/>, whose native surface is the Rx
/// <c>DataReceived</c> observable.
/// </summary>
public static class L2CapChannelAsyncExtensions
{
    /// <summary>
    /// Reads buffers off the channel until the remote peer closes it or
    /// <paramref name="cancellationToken"/> fires.
    /// </summary>
    /// <param name="channel">The open channel.</param>
    /// <param name="cancellationToken">Stops the enumeration.</param>
    /// <returns>The buffers as they arrive, in order.</returns>
    /// <remarks>
    /// Buffers arrive exactly as the platform delivered them - L2CAP CoC is a stream, not a message
    /// bus, so a logical message may span several buffers. Frame it yourself if you need boundaries.
    /// </remarks>
    public static async IAsyncEnumerable<byte[]> ReadAll(
        this L2CapChannel channel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));

        // unbounded: the platform already backpressures at the socket, and dropping BLE payloads
        // silently would be worse than the memory
        var buffer = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        using var subscription = channel.DataReceived.Subscribe(
            data => buffer.Writer.TryWrite(data),
            ex => buffer.Writer.TryComplete(ex),
            () => buffer.Writer.TryComplete()
        );

        await foreach (var data in buffer.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return data;
    }
}
