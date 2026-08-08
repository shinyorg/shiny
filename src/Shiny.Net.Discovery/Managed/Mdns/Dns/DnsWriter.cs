using System.Buffers.Binary;
using System.Text;

namespace Shiny.Net.Discovery.Managed.Dns;


/// <summary>
/// Writes DNS wire format with RFC 1035 name compression.
/// </summary>
sealed class DnsWriter
{
    /// <summary>A compression pointer only has 14 bits of offset.</summary>
    const int MaxPointerOffset = 0x3FFF;

    byte[] buffer = new byte[512];
    readonly Dictionary<string, int> nameOffsets = new(StringComparer.Ordinal);

    public int Position { get; private set; }


    void Ensure(int count)
    {
        if (this.Position + count > this.buffer.Length)
            Array.Resize(ref this.buffer, Math.Max(this.buffer.Length * 2, this.Position + count));
    }


    public void WriteByte(byte value)
    {
        this.Ensure(1);
        this.buffer[this.Position++] = value;
    }


    public void WriteUInt16(ushort value)
    {
        this.Ensure(2);
        BinaryPrimitives.WriteUInt16BigEndian(this.buffer.AsSpan(this.Position), value);
        this.Position += 2;
    }


    public void WriteUInt32(uint value)
    {
        this.Ensure(4);
        BinaryPrimitives.WriteUInt32BigEndian(this.buffer.AsSpan(this.Position), value);
        this.Position += 4;
    }


    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        this.Ensure(value.Length);
        value.CopyTo(this.buffer.AsSpan(this.Position));
        this.Position += value.Length;
    }


    public void WriteName(DnsName name)
    {
        for (var i = 0; i < name.Count; i++)
        {
            var key = name.SuffixKey(i);
            if (this.nameOffsets.TryGetValue(key, out var offset))
            {
                this.WriteUInt16((ushort)(0xC000 | offset));
                return;
            }

            if (this.Position <= MaxPointerOffset)
                this.nameOffsets[key] = this.Position;

            var bytes = Encoding.UTF8.GetBytes(name[i]);
            if (bytes.Length > 63)
                throw new MdnsException($"The DNS label '{name[i]}' encodes to {bytes.Length} bytes - the limit is 63.");

            this.WriteByte((byte)bytes.Length);
            this.WriteBytes(bytes);
        }
        this.WriteByte(0);
    }


    /// <summary>
    /// Writes a placeholder RDLENGTH and returns its offset for <see cref="PatchLength"/>.
    /// </summary>
    public int ReserveLength()
    {
        var offset = this.Position;
        this.WriteUInt16(0);
        return offset;
    }


    /// <summary>
    /// Back-fills an RDLENGTH reserved earlier with the number of bytes written since.
    /// </summary>
    public void PatchLength(int offset)
    {
        var length = this.Position - offset - 2;
        BinaryPrimitives.WriteUInt16BigEndian(this.buffer.AsSpan(offset), (ushort)length);
    }


    public byte[] ToArray() => this.buffer[..this.Position];
}
