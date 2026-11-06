using System.Buffers.Binary;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// The SMP operation carried in a frame header.
/// </summary>
public enum SmpOperation : byte
{
    /// <summary>A read request.</summary>
    Read = 0,

    /// <summary>The response to a read request.</summary>
    ReadResponse = 1,

    /// <summary>A write request.</summary>
    Write = 2,

    /// <summary>The response to a write request.</summary>
    WriteResponse = 3
}


/// <summary>
/// The SMP protocol version carried in a frame header.
/// </summary>
public enum SmpVersion : byte
{
    /// <summary>The original protocol - errors are a top level <c>rc</c>.</summary>
    V1 = 0,

    /// <summary>SMP version 2 - group errors are an <c>err</c> map of <c>group</c> + <c>rc</c>.</summary>
    V2 = 1
}


/// <summary>
/// The 8 byte header in front of every SMP frame. Multi-byte fields are big endian.
/// </summary>
/// <param name="Operation">The operation.</param>
/// <param name="Version">The protocol version.</param>
/// <param name="Flags">Header flags (unused - always 0).</param>
/// <param name="Length">The length of the CBOR payload that follows the header.</param>
/// <param name="Group">The management group id.</param>
/// <param name="Sequence">The sequence number that pairs a response with its request.</param>
/// <param name="Command">The command id within the group.</param>
public readonly record struct SmpHeader(
    SmpOperation Operation,
    SmpVersion Version,
    byte Flags,
    ushort Length,
    ushort Group,
    byte Sequence,
    byte Command
)
{
    /// <summary>The size of the header in bytes.</summary>
    public const int Size = 8;


    /// <summary>
    /// Writes the header to the first 8 bytes of <paramref name="destination"/>.
    /// </summary>
    public void Write(Span<byte> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException("The destination is smaller than an SMP header", nameof(destination));

        destination[0] = (byte)((((byte)this.Version & 0x03) << 3) | ((byte)this.Operation & 0x07));
        destination[1] = this.Flags;
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], this.Length);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..], this.Group);
        destination[6] = this.Sequence;
        destination[7] = this.Command;
    }


    /// <summary>
    /// Reads a header from the first 8 bytes of <paramref name="source"/>.
    /// </summary>
    public static SmpHeader Read(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size)
            throw new ArgumentException("The source is smaller than an SMP header", nameof(source));

        return new SmpHeader(
            (SmpOperation)(source[0] & 0x07),
            (SmpVersion)((source[0] >> 3) & 0x03),
            source[1],
            BinaryPrimitives.ReadUInt16BigEndian(source[2..]),
            BinaryPrimitives.ReadUInt16BigEndian(source[4..]),
            source[6],
            source[7]
        );
    }


    /// <summary>
    /// Returns the total frame length (header + payload) when <paramref name="buffer"/> holds at least a header,
    /// otherwise null. Transports use this to reassemble a frame that arrives in several pieces.
    /// </summary>
    public static int? GetFrameLength(ReadOnlySpan<byte> buffer)
        => buffer.Length < Size ? null : Size + BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);
}
