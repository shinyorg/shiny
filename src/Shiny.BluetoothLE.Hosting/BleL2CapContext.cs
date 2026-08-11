using System;
using System.Collections.Generic;
using System.Threading;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Per-channel state for a hosted L2CAP listener. One instance per accepted central connection.
/// </summary>
public sealed class BleL2CapContext
{
    Dictionary<string, object?>? items;


    /// <summary>
    /// Creates a new instance. Called by generated code.
    /// </summary>
    /// <param name="channel">The accepted channel.</param>
    public BleL2CapContext(L2CapChannel channel)
        => this.Channel = channel ?? throw new ArgumentNullException(nameof(channel));


    /// <summary>
    /// Gets the accepted channel.
    /// </summary>
    public L2CapChannel Channel { get; }

    /// <summary>
    /// Gets the PSM the channel is running on.
    /// </summary>
    public ushort Psm => this.Channel.Psm;

    /// <summary>
    /// Gets the remote peer identifier (peripheral UUID on Apple, MAC address on Android).
    /// </summary>
    public string PeerIdentifier => this.Channel.Identifier;

    /// <summary>
    /// Gets a loosely typed bag scoped to this channel. Created on first access.
    /// </summary>
    public IDictionary<string, object?> Items
    {
        get
        {
            var current = this.items;
            if (current != null)
                return current;

            var created = new Dictionary<string, object?>(StringComparer.Ordinal);
            return Interlocked.CompareExchange(ref this.items, created, null) ?? created;
        }
    }
}
