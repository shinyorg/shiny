using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


/// <summary>
/// Splits a message across as many GATT writes or notifications as the ATT MTU needs, and puts it back
/// together on the far side.
/// </summary>
/// <remarks>
/// <para>A single write or notification carries at most <c>IPeripheral.Mtu</c> bytes - the ATT MTU minus its 3-byte
/// header - which can be as small as 20.
/// Anything longer - a JSON command, a certificate, a scan result - has to be split, and a receiver has to
/// know where one message ends and the next begins.</para>
/// <para>Every fragment starts with one header byte:</para>
/// <code>
///   bit 7  START  - first fragment of a message
///   bit 6  END    - last fragment of a message
///   bits 0-5      - sequence number, counting fragments within the message and wrapping at 64
/// </code>
/// <para>A message that fits in one fragment has both START and END set, so short messages cost one byte.
/// The sequence number lets <see cref="BleMessageReassembler"/> notice a dropped or reordered fragment and
/// abandon the message rather than hand back garbage. The same format is used in both directions, and by
/// the hosting side (<c>NotifyMessage</c>, and <c>[RequestResponseCharacteristic(Framed = true)]</c>) and
/// the central side (<c>WriteCharacteristicMessageAsync</c>, <c>NotifyCharacteristicMessages</c>).</para>
/// </remarks>
public static class BleMessageFraming
{
    const byte FlagStart = 0x80;
    const byte FlagEnd = 0x40;
    const byte SequenceMask = 0x3F;

    /// <summary>The smallest payload a single GATT operation carries: a 23-byte ATT MTU less its 3-byte header.</summary>
    public const int MinimumPayload = 20;

    /// <summary>The per-fragment header this format adds.</summary>
    public const int HeaderSize = 1;

    /// <summary>The largest message a <see cref="BleMessageReassembler"/> accepts unless told otherwise.</summary>
    public const int DefaultMaxMessageBytes = 64 * 1024;


    /// <summary>
    /// How many message bytes fit in one fragment.
    /// </summary>
    /// <param name="mtu">
    /// The bytes one GATT operation carries - <c>IPeripheral.Mtu</c> as either side reports it, which already excludes the
    /// ATT header. Anything below <see cref="MinimumPayload"/> is treated as the minimum.
    /// </param>
    public static int PayloadPerFragment(int mtu)
        => Math.Max(MinimumPayload, mtu) - HeaderSize;


    /// <summary>
    /// Splits a message into fragments sized for the negotiated MTU.
    /// </summary>
    /// <param name="message">The complete message. An empty message still produces one fragment.</param>
    /// <param name="mtu">The bytes one GATT operation carries - pass <c>IPeripheral.Mtu</c> unchanged.</param>
    /// <returns>The fragments, in the order they must be sent.</returns>
    public static IReadOnlyList<byte[]> Encode(ReadOnlySpan<byte> message, int mtu)
    {
        var usable = PayloadPerFragment(mtu);
        var count = message.Length == 0 ? 1 : (message.Length + usable - 1) / usable;

        var fragments = new List<byte[]>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = i * usable;
            var length = Math.Max(0, Math.Min(usable, message.Length - offset));

            var header = (byte)(i & SequenceMask);
            if (i == 0)
                header |= FlagStart;
            if (i == count - 1)
                header |= FlagEnd;

            var fragment = new byte[HeaderSize + length];
            fragment[0] = header;
            if (length > 0)
                message.Slice(offset, length).CopyTo(fragment.AsSpan(HeaderSize));

            fragments.Add(fragment);
        }
        return fragments;
    }


    internal static bool IsStart(byte header) => (header & FlagStart) != 0;
    internal static bool IsEnd(byte header) => (header & FlagEnd) != 0;
    internal static int Sequence(byte header) => header & SequenceMask;
    internal static int NextSequence(int sequence) => (sequence + 1) & SequenceMask;
}


/// <summary>
/// Puts messages split by <see cref="BleMessageFraming.Encode"/> back together.
/// </summary>
/// <remarks>
/// State is per link and per characteristic: keep one instance for each central (or peripheral) and each
/// direction, never one shared between them, or two senders' fragments interleave. Not thread-safe -
/// fragments on one link arrive in order, so callers already feed it one at a time.
/// </remarks>
public sealed class BleMessageReassembler
{
    readonly List<byte> buffer = new();
    readonly int maxMessageBytes;
    int expectedSequence;
    bool inMessage;


    /// <param name="maxMessageBytes">
    /// The largest message accepted. A sender that exceeds it has the message abandoned rather than buffered -
    /// without a cap, an unauthenticated central could stream fragments until the host runs out of memory.
    /// </param>
    public BleMessageReassembler(int maxMessageBytes = BleMessageFraming.DefaultMaxMessageBytes)
    {
        if (maxMessageBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxMessageBytes), "maxMessageBytes must be at least 1");

        this.maxMessageBytes = maxMessageBytes;
    }


    /// <summary>Gets whether part of a message has arrived and the rest is still expected.</summary>
    public bool IsInMessage => this.inMessage;


    /// <summary>Discards any partially received message.</summary>
    public void Reset()
    {
        this.buffer.Clear();
        this.inMessage = false;
        this.expectedSequence = 0;
    }


    /// <summary>
    /// Feeds one received fragment.
    /// </summary>
    /// <param name="fragment">The bytes of one write or notification.</param>
    /// <param name="message">The complete message when this fragment finished one; otherwise null.</param>
    /// <returns>
    /// <see cref="BleMessageFrameResult.Complete"/> with <paramref name="message"/> set,
    /// <see cref="BleMessageFrameResult.Partial"/> while a message is still arriving, or an error - after which
    /// the partial message has already been discarded, so the next START fragment begins cleanly.
    /// </returns>
    public BleMessageFrameResult Push(ReadOnlySpan<byte> fragment, out byte[]? message)
    {
        message = null;

        if (fragment.Length < BleMessageFraming.HeaderSize)
        {
            this.Reset();
            return BleMessageFrameResult.Malformed;
        }

        var header = fragment[0];
        var body = fragment.Slice(BleMessageFraming.HeaderSize);

        if (BleMessageFraming.IsStart(header))
        {
            // a START in the middle of a message means the sender gave up on the last one and began again
            this.buffer.Clear();
            this.inMessage = true;
            this.expectedSequence = 0;
        }
        else if (!this.inMessage)
        {
            return BleMessageFrameResult.OutOfSequence;
        }

        if (BleMessageFraming.Sequence(header) != this.expectedSequence)
        {
            this.Reset();
            return BleMessageFrameResult.OutOfSequence;
        }

        if (this.buffer.Count + body.Length > this.maxMessageBytes)
        {
            this.Reset();
            return BleMessageFrameResult.TooLarge;
        }

        for (var i = 0; i < body.Length; i++)
            this.buffer.Add(body[i]);

        this.expectedSequence = BleMessageFraming.NextSequence(this.expectedSequence);

        if (!BleMessageFraming.IsEnd(header))
            return BleMessageFrameResult.Partial;

        message = this.buffer.ToArray();
        this.Reset();
        return BleMessageFrameResult.Complete;
    }
}


/// <summary>
/// What feeding a fragment to <see cref="BleMessageReassembler"/> produced.
/// </summary>
public enum BleMessageFrameResult
{
    /// <summary>The fragment was accepted and more are expected.</summary>
    Partial,

    /// <summary>The fragment finished a message.</summary>
    Complete,

    /// <summary>The fragment was too short to carry a header.</summary>
    Malformed,

    /// <summary>A fragment was missing, reordered, or arrived with no START before it.</summary>
    OutOfSequence,

    /// <summary>The message grew past the reassembler's size cap.</summary>
    TooLarge
}
