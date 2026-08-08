using System.Buffers.Binary;
using System.Text;

namespace Shiny.Net.Discovery.Managed.Dns;


/// <summary>
/// Reads DNS wire format, following RFC 1035 compression pointers.
/// </summary>
sealed class DnsReader(byte[] data)
{
    /// <summary>A malformed packet can point at itself - bail out well before stack/CPU damage.</summary>
    const int MaxPointerJumps = 64;

    public int Position { get; set; }

    public int Length => data.Length;

    public bool HasRemaining => this.Position < data.Length;


    public byte ReadByte()
    {
        this.Require(1);
        return data[this.Position++];
    }


    public ushort ReadUInt16()
    {
        this.Require(2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(this.Position));
        this.Position += 2;
        return value;
    }


    public uint ReadUInt32()
    {
        this.Require(4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(this.Position));
        this.Position += 4;
        return value;
    }


    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        this.Require(count);
        var span = data.AsSpan(this.Position, count);
        this.Position += count;
        return span;
    }


    public DnsName ReadName()
    {
        var labels = new List<string>();
        var current = this.Position;
        var jumps = 0;
        int? resume = null;

        while (true)
        {
            if (current >= data.Length)
                throw new DnsFormatException("The name ran past the end of the packet.");

            var length = data[current];

            if ((length & 0xC0) == 0xC0)
            {
                if (current + 1 >= data.Length)
                    throw new DnsFormatException("A compression pointer ran past the end of the packet.");

                if (++jumps > MaxPointerJumps)
                    throw new DnsFormatException("The name contains a compression pointer loop.");

                var pointer = ((length & 0x3F) << 8) | data[current + 1];
                resume ??= current + 2;
                current = pointer;
            }
            else if (length == 0)
            {
                resume ??= current + 1;
                break;
            }
            else
            {
                if (current + 1 + length > data.Length)
                    throw new DnsFormatException("A label ran past the end of the packet.");

                labels.Add(Encoding.UTF8.GetString(data, current + 1, length));
                current += 1 + length;
            }
        }

        this.Position = resume.Value;
        return new DnsName(labels.ToArray());
    }


    void Require(int count)
    {
        if (this.Position + count > data.Length)
            throw new DnsFormatException($"Needed {count} more byte(s) at offset {this.Position} but the packet is {data.Length} bytes.");
    }
}
