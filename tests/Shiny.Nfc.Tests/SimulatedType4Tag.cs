using System.Buffers.Binary;
using Shiny.Nfc.Infrastructure.Tags;

namespace Shiny.Nfc.Tests;


/// <summary>
/// An NFC Forum Type 4 tag (mapping 2.0): the NDEF Tag Application with a capability container
/// file and an NDEF file, answering SELECT, READ BINARY and UPDATE BINARY. Rejects reads and
/// writes larger than the MLe/MLc it advertises, as strict tags do.
/// </summary>
class SimulatedType4Tag : IApduChannel
{
    /// <summary>The ATR a PC/SC reader gives an ISO 14443-4 card - anything that is not a storage card ATR.</summary>
    public static readonly byte[] IsoDepAtr = Convert.FromHexString("3B8180018080");

    static readonly byte[] AidV2 = [0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01];
    static readonly byte[] AidV1 = [0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x00];
    const ushort CcFileId = 0xE103;

    bool applicationSelected;
    ushort? selectedFile;

    public bool AnswersV2 { get; init; } = true;
    public bool AnswersV1 { get; init; } = true;
    public bool HasNdefApplication { get; init; } = true;
    public bool AnswersGetUid { get; init; }
    public ushort MaxLe { get; init; } = 0x3B;
    public ushort MaxLc { get; init; } = 0x34;
    public ushort NdefFileId { get; init; } = 0xE104;
    public byte WriteAccess { get; init; }
    public byte[] NdefFile { get; init; } = new byte[0x800];

    public List<(int Offset, byte[] Data)> Updates { get; } = new();


    public byte[] CapabilityContainer
    {
        get
        {
            var cc = new byte[15];
            BinaryPrimitives.WriteUInt16BigEndian(cc, 15);
            cc[2] = 0x20;
            BinaryPrimitives.WriteUInt16BigEndian(cc.AsSpan(3), this.MaxLe);
            BinaryPrimitives.WriteUInt16BigEndian(cc.AsSpan(5), this.MaxLc);
            cc[7] = 0x04;
            cc[8] = 0x06;
            BinaryPrimitives.WriteUInt16BigEndian(cc.AsSpan(9), this.NdefFileId);
            BinaryPrimitives.WriteUInt16BigEndian(cc.AsSpan(11), (ushort)this.NdefFile.Length);
            cc[13] = 0x00;
            cc[14] = this.WriteAccess;
            return cc;
        }
    }


    public int StoredLength => BinaryPrimitives.ReadUInt16BigEndian(this.NdefFile);
    public byte[] StoredMessage => this.NdefFile[2..(2 + this.StoredLength)];


    public void Store(NdefMessage message)
    {
        var bytes = message.ToByteArray();
        BinaryPrimitives.WriteUInt16BigEndian(this.NdefFile, (ushort)bytes.Length);
        bytes.CopyTo(this.NdefFile, 2);
    }


    public ValueTask<byte[]> Transmit(byte[] command, CancellationToken ct) => ValueTask.FromResult(this.Handle(command));


    byte[] Handle(byte[] c)
    {
        if (c is [0xFF, 0xCA, 0x00, 0x00, 0x00])
            return this.AnswersGetUid ? [0x08, 0x12, 0x34, 0x56, 0x90, 0x00] : [0x6A, 0x81];

        // SELECT by name
        if (c.Length >= 12 && c[0] == 0x00 && c[1] == 0xA4 && c[2] == 0x04 && c[4] == 0x07)
        {
            var aid = c[5..12];
            var ok = this.HasNdefApplication
                     && ((this.AnswersV2 && aid.SequenceEqual(AidV2)) || (this.AnswersV1 && aid.SequenceEqual(AidV1)));

            this.applicationSelected = ok;
            this.selectedFile = null;
            return ok ? [0x90, 0x00] : [0x6A, 0x82];
        }

        // SELECT by file ID
        if (c is [0x00, 0xA4, 0x00, _, 0x02, var hi, var lo])
        {
            var fileId = (ushort)((hi << 8) | lo);
            if (!this.applicationSelected || (fileId != CcFileId && fileId != this.NdefFileId))
                return [0x6A, 0x82];

            this.selectedFile = fileId;
            return [0x90, 0x00];
        }

        // READ BINARY
        if (c is [0x00, 0xB0, var p1, var p2, var le] && this.selectedFile is { } file)
        {
            var content = file == CcFileId ? this.CapabilityContainer : this.NdefFile;
            var offset = (p1 << 8) | p2;
            var length = le == 0 ? 256 : le;

            if (file != CcFileId && length > this.MaxLe)
                return [0x67, 0x00];
            if (offset >= content.Length)
                return [0x6B, 0x00];

            var take = Math.Min(length, content.Length - offset);
            return [.. content.AsSpan(offset, take), 0x90, 0x00];
        }

        // UPDATE BINARY
        if (c.Length > 5 && c[0] == 0x00 && c[1] == 0xD6 && this.selectedFile == this.NdefFileId)
        {
            var offset = (c[2] << 8) | c[3];
            var length = c[4];
            if (this.WriteAccess != 0x00)
                return [0x69, 0x82];
            if (length > this.MaxLc || c.Length != 5 + length)
                return [0x67, 0x00];
            if (offset + length > this.NdefFile.Length)
                return [0x6B, 0x00];

            var data = c[5..];
            data.CopyTo(this.NdefFile, offset);
            this.Updates.Add((offset, data));
            return [0x90, 0x00];
        }

        return [0x6D, 0x00];
    }
}
