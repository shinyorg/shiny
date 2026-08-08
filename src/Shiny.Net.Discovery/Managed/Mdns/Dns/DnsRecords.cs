using System.Net;
using System.Net.Sockets;
using Shiny.Net.Discovery.Internals;

namespace Shiny.Net.Discovery.Managed.Dns;


sealed class DnsQuestion
{
    public required DnsName Name { get; init; }

    public DnsRecordType Type { get; init; } = DnsRecordType.ANY;

    public DnsRecordClass Class { get; init; } = DnsRecordClass.IN;

    /// <summary>
    /// The mDNS "QU" bit - asks the responder to answer by unicast instead of multicast.
    /// </summary>
    public bool UnicastResponse { get; init; }


    public void Write(DnsWriter writer)
    {
        writer.WriteName(this.Name);
        writer.WriteUInt16((ushort)this.Type);

        var cls = (ushort)this.Class;
        if (this.UnicastResponse)
            cls |= 0x8000;

        writer.WriteUInt16(cls);
    }


    public static DnsQuestion Read(DnsReader reader)
    {
        var name = reader.ReadName();
        var type = (DnsRecordType)reader.ReadUInt16();
        var rawClass = reader.ReadUInt16();

        return new DnsQuestion
        {
            Name = name,
            Type = type,
            Class = (DnsRecordClass)(rawClass & 0x7FFF),
            UnicastResponse = (rawClass & 0x8000) != 0
        };
    }


    public override string ToString() => $"{this.Type} {this.Name}";
}


abstract class DnsResourceRecord
{
    public required DnsName Name { get; init; }

    public abstract DnsRecordType Type { get; }

    public DnsRecordClass Class { get; init; } = DnsRecordClass.IN;

    /// <summary>
    /// The mDNS "cache flush" bit - tells receivers to replace rather than merge with what
    /// they already hold for this name/type.
    /// </summary>
    public bool CacheFlush { get; init; }

    /// <summary>
    /// Seconds the record stays valid. Zero is a "goodbye" - the record is going away.
    /// </summary>
    public uint Ttl { get; init; }

    public bool IsGoodbye => this.Ttl == 0;


    protected abstract void WriteData(DnsWriter writer);


    public void Write(DnsWriter writer)
    {
        writer.WriteName(this.Name);
        writer.WriteUInt16((ushort)this.Type);

        var cls = (ushort)this.Class;
        if (this.CacheFlush)
            cls |= 0x8000;

        writer.WriteUInt16(cls);
        writer.WriteUInt32(this.Ttl);

        var lengthOffset = writer.ReserveLength();
        this.WriteData(writer);
        writer.PatchLength(lengthOffset);
    }


    public static DnsResourceRecord Read(DnsReader reader)
    {
        var name = reader.ReadName();
        var type = (DnsRecordType)reader.ReadUInt16();
        var rawClass = reader.ReadUInt16();
        var ttl = reader.ReadUInt32();
        var dataLength = reader.ReadUInt16();

        if (reader.Position + dataLength > reader.Length)
            throw new DnsFormatException($"The RDATA for '{name}' claims {dataLength} bytes but the packet is too short.");

        var dataEnd = reader.Position + dataLength;
        var cls = (DnsRecordClass)(rawClass & 0x7FFF);
        var cacheFlush = (rawClass & 0x8000) != 0;

        DnsResourceRecord record = type switch
        {
            DnsRecordType.PTR => new PtrRecord
            {
                Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
                Target = reader.ReadName()
            },
            DnsRecordType.SRV => ReadSrv(reader, name, cls, cacheFlush, ttl),
            DnsRecordType.TXT => new TxtRecord
            {
                Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
                Data = reader.ReadBytes(dataLength).ToArray()
            },
            DnsRecordType.A when dataLength == 4 => new AddressRecord
            {
                Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
                Address = new IPAddress(reader.ReadBytes(4))
            },
            DnsRecordType.AAAA when dataLength == 16 => new AddressRecord
            {
                Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
                Address = new IPAddress(reader.ReadBytes(16))
            },
            _ => new UnknownRecord
            {
                Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
                RawType = type, Data = reader.ReadBytes(dataLength).ToArray()
            }
        };

        // RDATA can hold compressed names, so never trust where the reader landed
        reader.Position = dataEnd;
        return record;
    }


    static SrvRecord ReadSrv(DnsReader reader, DnsName name, DnsRecordClass cls, bool cacheFlush, uint ttl)
    {
        var priority = reader.ReadUInt16();
        var weight = reader.ReadUInt16();
        var port = reader.ReadUInt16();

        return new SrvRecord
        {
            Name = name, Class = cls, CacheFlush = cacheFlush, Ttl = ttl,
            Priority = priority,
            Weight = weight,
            Port = port,
            Target = reader.ReadName()
        };
    }


    public override string ToString() => $"{this.Type} {this.Name} ttl={this.Ttl}";
}


sealed class PtrRecord : DnsResourceRecord
{
    public override DnsRecordType Type => DnsRecordType.PTR;

    public required DnsName Target { get; init; }

    protected override void WriteData(DnsWriter writer) => writer.WriteName(this.Target);

    public override string ToString() => $"PTR {this.Name} -> {this.Target} ttl={this.Ttl}";
}


sealed class SrvRecord : DnsResourceRecord
{
    public override DnsRecordType Type => DnsRecordType.SRV;

    public ushort Priority { get; init; }

    public ushort Weight { get; init; }

    public required ushort Port { get; init; }

    public required DnsName Target { get; init; }

    protected override void WriteData(DnsWriter writer)
    {
        writer.WriteUInt16(this.Priority);
        writer.WriteUInt16(this.Weight);
        writer.WriteUInt16(this.Port);

        // RFC 2782 says SRV targets must not be compressed; Apple's responder compresses them
        // anyway and every implementation reads them, so compression here is safe and smaller.
        writer.WriteName(this.Target);
    }

    public override string ToString() => $"SRV {this.Name} -> {this.Target}:{this.Port} ttl={this.Ttl}";
}


sealed class TxtRecord : DnsResourceRecord
{
    IReadOnlyDictionary<string, string>? values;

    public override DnsRecordType Type => DnsRecordType.TXT;

    /// <summary>The raw RDATA - a sequence of length prefixed strings.</summary>
    public required byte[] Data { get; init; }

    /// <summary>The RDATA decoded into key/value pairs, cached after the first read.</summary>
    public IReadOnlyDictionary<string, string> Values
        => this.values ??= TxtRecordCodec.Decode(this.Data);

    protected override void WriteData(DnsWriter writer) => writer.WriteBytes(this.Data);

    public override string ToString() => $"TXT {this.Name} ({this.Values.Count} pair(s)) ttl={this.Ttl}";
}


sealed class AddressRecord : DnsResourceRecord
{
    public override DnsRecordType Type
        => this.Address.AddressFamily == AddressFamily.InterNetworkV6 ? DnsRecordType.AAAA : DnsRecordType.A;

    public required IPAddress Address { get; init; }

    protected override void WriteData(DnsWriter writer) => writer.WriteBytes(this.Address.GetAddressBytes());

    public override string ToString() => $"{this.Type} {this.Name} -> {this.Address} ttl={this.Ttl}";
}


sealed class UnknownRecord : DnsResourceRecord
{
    public override DnsRecordType Type => this.RawType;

    public required DnsRecordType RawType { get; init; }

    public required byte[] Data { get; init; }

    protected override void WriteData(DnsWriter writer) => writer.WriteBytes(this.Data);
}
