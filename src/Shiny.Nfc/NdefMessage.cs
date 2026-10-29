using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Shiny.Nfc;


/// <summary>
/// An NDEF message - one or more <see cref="NdefRecord"/>s, encoded as the NFC Forum NDEF 1.0
/// binary format.
/// </summary>
/// <remarks>
/// Messages are always encoded unchunked, using the short-record form for any payload under 256
/// bytes. Parsing accepts the whole format, chunked records included, and is strict: a missing
/// message-begin flag, a truncated record or bytes after the message-end record are all errors,
/// because on a tag they mean the bytes are not what they claim to be.
/// </remarks>
public sealed class NdefMessage : IEquatable<NdefMessage>
{
    const byte FlagMb = 0x80;
    const byte FlagMe = 0x40;
    const byte FlagCf = 0x20;
    const byte FlagSr = 0x10;
    const byte FlagIl = 0x08;
    const byte TnfMask = 0x07;
    const byte TnfUnchanged = 0x06;
    const byte TnfReserved = 0x07;


    /// <summary>The empty message - a single <see cref="NdefRecord.Empty"/> record.</summary>
    public static NdefMessage Empty { get; } = new(NdefRecord.Empty);


    /// <summary>Creates a message from its records, in order.</summary>
    /// <exception cref="ArgumentException">No records were given - use <see cref="Empty"/> for an empty message.</exception>
    public NdefMessage(params IEnumerable<NdefRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var list = records.ToArray();
        if (list.Length == 0)
            throw new ArgumentException("An NDEF message has at least one record - use NdefMessage.Empty", nameof(records));

        if (list.Any(x => x is null))
            throw new ArgumentException("An NDEF message cannot contain a null record", nameof(records));

        this.Records = list;
    }


    /// <summary>The records, in order.</summary>
    public IReadOnlyList<NdefRecord> Records { get; }

    /// <summary>True when this is the empty message - a single empty record.</summary>
    public bool IsEmpty => this.Records.Count == 1 && this.Records[0].TypeNameFormat == NdefTypeNameFormat.Empty;

    /// <summary>The size of the encoded message in bytes - what it takes up on a tag.</summary>
    public int Length => this.Records.Sum(EncodedLength);


    /// <summary>Encodes the message.</summary>
    public byte[] ToByteArray()
    {
        var buffer = new byte[this.Length];
        var pos = 0;

        for (var i = 0; i < this.Records.Count; i++)
        {
            var record = this.Records[i];
            var shortRecord = record.Payload.Length < 256;

            var header = (byte)record.TypeNameFormat;
            if (i == 0)
                header |= FlagMb;
            if (i == this.Records.Count - 1)
                header |= FlagMe;
            if (shortRecord)
                header |= FlagSr;
            if (!record.Id.IsEmpty)
                header |= FlagIl;

            buffer[pos++] = header;
            buffer[pos++] = (byte)record.Type.Length;

            if (shortRecord)
            {
                buffer[pos++] = (byte)record.Payload.Length;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(pos), (uint)record.Payload.Length);
                pos += 4;
            }
            if (!record.Id.IsEmpty)
                buffer[pos++] = (byte)record.Id.Length;

            record.Type.Span.CopyTo(buffer.AsSpan(pos));
            pos += record.Type.Length;
            record.Id.Span.CopyTo(buffer.AsSpan(pos));
            pos += record.Id.Length;
            record.Payload.Span.CopyTo(buffer.AsSpan(pos));
            pos += record.Payload.Length;
        }
        return buffer;
    }


    /// <summary>Parses an encoded message.</summary>
    /// <exception cref="NdefFormatException">The bytes are not exactly one valid NDEF message.</exception>
    public static NdefMessage Parse(ReadOnlySpan<byte> data)
    {
        if (!TryParse(data, out var message, out var error))
            throw new NdefFormatException(error);

        return message;
    }


    /// <summary>Parses an encoded message, returning false instead of throwing when the bytes are not valid.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, [NotNullWhen(true)] out NdefMessage? message)
        => TryParse(data, out message, out _);


    static bool TryParse(ReadOnlySpan<byte> data, [NotNullWhen(true)] out NdefMessage? message, out string error)
    {
        message = null;
        error = String.Empty;

        var records = new List<NdefRecord>();
        var pos = 0;
        var first = true;

        // a chunked record being reassembled
        List<byte>? chunkPayload = null;
        var chunkTnf = NdefTypeNameFormat.Empty;
        byte[] chunkType = [];
        byte[] chunkId = [];

        while (true)
        {
            if (pos >= data.Length)
                return Fail(out error, data.IsEmpty ? "No NDEF data" : "The NDEF message ends without a message-end record");

            var header = data[pos++];
            var mb = (header & FlagMb) != 0;
            var me = (header & FlagMe) != 0;
            var cf = (header & FlagCf) != 0;
            var sr = (header & FlagSr) != 0;
            var il = (header & FlagIl) != 0;
            var tnf = (byte)(header & TnfMask);

            if (first && !mb)
                return Fail(out error, "The first NDEF record does not have the message-begin flag");
            if (!first && mb)
                return Fail(out error, "An NDEF record after the first has the message-begin flag");
            first = false;

            var fixedLength = 1 + (sr ? 1 : 4) + (il ? 1 : 0);
            if (pos + fixedLength > data.Length)
                return Fail(out error, "An NDEF record header is truncated");

            int typeLength = data[pos++];
            long payloadLength;
            if (sr)
            {
                payloadLength = data[pos++];
            }
            else
            {
                payloadLength = BinaryPrimitives.ReadUInt32BigEndian(data[pos..]);
                pos += 4;
            }
            int idLength = il ? data[pos++] : 0;

            if (pos + typeLength + idLength + payloadLength > data.Length)
                return Fail(out error, "An NDEF record is longer than the data it was read from");

            var type = data.Slice(pos, typeLength);
            pos += typeLength;
            var id = data.Slice(pos, idLength);
            pos += idLength;
            var payload = data.Slice(pos, (int)payloadLength);
            pos += (int)payloadLength;

            if (chunkPayload != null)
            {
                // middle or terminating chunk
                if (tnf != TnfUnchanged)
                    return Fail(out error, "A chunk after the first does not have the Unchanged type name format");
                if (typeLength != 0 || il)
                    return Fail(out error, "A chunk after the first has a type or ID");

                chunkPayload.AddRange(payload);
                if (!cf)
                {
                    if (!TryCreate(chunkTnf, chunkType, chunkPayload.ToArray(), chunkId, records, out error))
                        return false;

                    chunkPayload = null;
                }
            }
            else
            {
                if (tnf == TnfUnchanged)
                    return Fail(out error, "An NDEF record has the Unchanged type name format outside a chunked record");
                if (tnf == TnfReserved)
                    return Fail(out error, "An NDEF record has the reserved type name format 7");

                if (cf)
                {
                    chunkPayload = [.. payload];
                    chunkTnf = (NdefTypeNameFormat)tnf;
                    chunkType = type.ToArray();
                    chunkId = id.ToArray();
                }
                else if (!TryCreate((NdefTypeNameFormat)tnf, type.ToArray(), payload.ToArray(), id.ToArray(), records, out error))
                {
                    return false;
                }
            }

            if (me)
            {
                if (cf)
                    return Fail(out error, "A chunk that is not the last has the message-end flag");
                break;
            }
        }

        if (pos != data.Length)
            return Fail(out error, $"{data.Length - pos} byte(s) follow the NDEF message-end record");

        message = new NdefMessage(records);
        return true;
    }


    static bool TryCreate(NdefTypeNameFormat tnf, byte[] type, byte[] payload, byte[] id, List<NdefRecord> records, out string error)
    {
        try
        {
            records.Add(new NdefRecord(tnf, type, payload, id));
            error = String.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }


    static bool Fail(out string error, string message)
    {
        error = message;
        return false;
    }


    static int EncodedLength(NdefRecord record)
        => 2                                            // header, type length
           + (record.Payload.Length < 256 ? 1 : 4)      // payload length
           + (record.Id.IsEmpty ? 0 : 1)                // ID length
           + record.Type.Length
           + record.Id.Length
           + record.Payload.Length;


    /// <inheritdoc />
    public bool Equals(NdefMessage? other) => other is not null && this.Records.SequenceEqual(other.Records);

    /// <inheritdoc />
    public override bool Equals(object? obj) => this.Equals(obj as NdefMessage);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var record in this.Records)
            hash.Add(record);

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => $"NdefMessage ({this.Records.Count} record(s), {this.Length} bytes)";
}
