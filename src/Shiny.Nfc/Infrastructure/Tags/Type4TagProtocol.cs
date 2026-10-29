using System.Buffers.Binary;

namespace Shiny.Nfc.Infrastructure.Tags;


/// <summary>
/// NFC Forum Type 4 Tag (ISO 14443-4 / ISO-DEP - DESFire, NTAG 424, phones emulating a tag) over
/// ISO 7816-4 APDUs.
/// </summary>
/// <remarks>
/// <para>Select the NDEF Tag Application, read the capability container file (E103) for the NDEF
/// file's ID, size, access rights and the largest read/write the tag accepts, then select that file
/// and read or update it. The file starts with the message length (NLEN, or 4-byte ENLEN for
/// mapping 3.0) followed by the message.</para>
/// <para>Mapping 2.0 and 3.0 tags are spoken to with the 2.0 application ID; anything that refuses it
/// gets one retry with the 1.0 ID, which older tags and some phone emulators still answer to.</para>
/// <para>Writes set the length to 0, write the message, then write the real length, so an
/// interrupted write leaves an empty tag rather than a corrupt one.</para>
/// </remarks>
sealed class Type4TagProtocol : INdefTagProtocol
{
    static readonly byte[] SelectApplicationV2 = [0x00, 0xA4, 0x04, 0x00, 0x07, 0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01, 0x00];
    static readonly byte[] SelectApplicationV1 = [0x00, 0xA4, 0x04, 0x00, 0x07, 0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x00];
    const ushort CapabilityContainerFileId = 0xE103;
    const ushort SecurityStatusNotSatisfied = 0x6982;
    const int MaxShortOffset = 0x7FFF;

    readonly IApduChannel channel;
    readonly bool legacy;
    int maxRead = 0x0F;
    int maxWrite = 0x0F;
    int fileSize;
    int lengthSize;
    bool writeGranted;
    int messageLength;


    Type4TagProtocol(IApduChannel channel, bool legacy)
    {
        this.channel = channel;
        this.legacy = legacy;
    }


    public static async Task<INdefTagProtocol> Open(IApduChannel channel, CancellationToken ct)
    {
        var legacy = false;
        if (!(await channel.Exchange(SelectApplicationV2, ct).ConfigureAwait(false)).IsSuccess)
        {
            if (!(await channel.Exchange(SelectApplicationV1, ct).ConfigureAwait(false)).IsSuccess)
                return new UnsupportedTagProtocol("this ISO-DEP tag - it has no NDEF application");

            legacy = true;
        }

        var protocol = new Type4TagProtocol(channel, legacy);
        return await protocol.ReadCapabilityContainer(ct).ConfigureAwait(false)
            ? protocol
            : new UnsupportedTagProtocol("this ISO-DEP tag - its NDEF capability container is missing or unreadable");
    }


    public bool IsNdefFormatted => true;
    public bool IsWritable => this.writeGranted;
    public int? MaxSize => this.fileSize - this.lengthSize;


    public async Task<NdefMessage?> ReadNdef(CancellationToken ct)
    {
        if (this.messageLength == 0)
            return null;

        if (this.messageLength > this.MaxSize)
            throw new NdefFormatException($"The tag claims a {this.messageLength}-byte message in a {this.fileSize}-byte NDEF file");

        var bytes = await this.ReadBinary(this.lengthSize, this.messageLength, ct).ConfigureAwait(false);
        return NdefMessage.Parse(bytes);
    }


    public async Task WriteNdef(NdefMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!this.writeGranted)
            throw new NfcTagReadOnlyException();

        var encoded = message.ToByteArray();
        var max = this.MaxSize!.Value;
        if (encoded.Length > max)
            throw new NdefMessageTooLargeException(encoded.Length, max);

        var length = new byte[this.lengthSize];
        await this.UpdateBinary(0, length, ct).ConfigureAwait(false);
        this.messageLength = 0;

        await this.UpdateBinary(this.lengthSize, encoded, ct).ConfigureAwait(false);

        if (this.lengthSize == 2)
            BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)encoded.Length);
        else
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)encoded.Length);

        await this.UpdateBinary(0, length, ct).ConfigureAwait(false);
        this.messageLength = encoded.Length;
    }


    public Task MakeReadOnly(CancellationToken ct)
        => throw new NfcNotSupportedException(
            "Type 4 tags have no standard way to be locked - each vendor has its own command, so use the tag vendor's tooling",
            NfcCapabilities.MakeReadOnly
        );


    async Task<bool> ReadCapabilityContainer(CancellationToken ct)
    {
        if (!await this.SelectFile(CapabilityContainerFileId, ct).ConfigureAwait(false))
            return false;

        var ccLength = BinaryPrimitives.ReadUInt16BigEndian(await this.ReadBinary(0, 2, ct).ConfigureAwait(false));
        if (ccLength < 15)
            return false;

        var cc = await this.ReadBinary(0, Math.Min((int)ccLength, 0xFF), ct).ConfigureAwait(false);
        var maxLe = BinaryPrimitives.ReadUInt16BigEndian(cc.AsSpan(3));
        var maxLc = BinaryPrimitives.ReadUInt16BigEndian(cc.AsSpan(5));

        ushort fileId;
        byte readAccess;
        byte writeAccess;
        switch (cc[7])
        {
            case 0x04 when cc[8] == 6:
                fileId = BinaryPrimitives.ReadUInt16BigEndian(cc.AsSpan(9));
                this.fileSize = BinaryPrimitives.ReadUInt16BigEndian(cc.AsSpan(11));
                readAccess = cc[13];
                writeAccess = cc[14];
                this.lengthSize = 2;
                break;

            // mapping 3.0 extended NDEF file control TLV - 4-byte size
            case 0x06 when cc[8] == 8 && cc.Length >= 17:
                fileId = BinaryPrimitives.ReadUInt16BigEndian(cc.AsSpan(9));
                this.fileSize = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(cc.AsSpan(11)), int.MaxValue);
                readAccess = cc[15];
                writeAccess = cc[16];
                this.lengthSize = 4;
                break;

            default:
                return false;
        }
        if (readAccess != 0x00 || this.fileSize <= this.lengthSize)
            return false;

        // short APDUs - Le and Lc are one byte
        this.maxRead = Math.Clamp((int)maxLe, 1, 0xFF);
        this.maxWrite = Math.Clamp((int)maxLc, 1, 0xFF);
        this.writeGranted = writeAccess == 0x00;

        if (!await this.SelectFile(fileId, ct).ConfigureAwait(false))
            throw new NfcException($"The tag's capability container names NDEF file {fileId:X4}, which cannot be selected");

        var length = await this.ReadBinary(0, this.lengthSize, ct).ConfigureAwait(false);
        this.messageLength = this.lengthSize == 2
            ? BinaryPrimitives.ReadUInt16BigEndian(length)
            : (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(length), int.MaxValue);

        return true;
    }


    async Task<bool> SelectFile(ushort fileId, CancellationToken ct)
    {
        // mapping 2.0 selects by file ID with "first or only occurrence, no response data" (P2 0C); 1.0 used P2 00
        byte[] command = [0x00, 0xA4, 0x00, this.legacy ? (byte)0x00 : (byte)0x0C, 0x02, (byte)(fileId >> 8), (byte)fileId];
        return (await this.channel.Exchange(command, ct).ConfigureAwait(false)).IsSuccess;
    }


    async Task<byte[]> ReadBinary(int offset, int count, CancellationToken ct)
    {
        var result = new byte[count];
        var read = 0;
        while (read < count)
        {
            var at = offset + read;
            if (at > MaxShortOffset)
                throw new NfcNotSupportedException("NDEF files larger than 32KB are not supported", NfcCapabilities.ReadNdef);

            var length = Math.Min(this.maxRead, count - read);
            var data = await this.channel.Send([0x00, 0xB0, (byte)(at >> 8), (byte)at, (byte)length], $"Reading the NDEF file at {at}", ct).ConfigureAwait(false);
            if (data.Length == 0)
                throw new NfcException($"Reading the NDEF file at {at} returned no data");

            var take = Math.Min(data.Length, count - read);
            Array.Copy(data, 0, result, read, take);
            read += take;
        }
        return result;
    }


    async Task UpdateBinary(int offset, byte[] data, CancellationToken ct)
    {
        for (var written = 0; written < data.Length;)
        {
            var at = offset + written;
            if (at > MaxShortOffset)
                throw new NfcNotSupportedException("NDEF files larger than 32KB are not supported", NfcCapabilities.WriteNdef);

            var length = Math.Min(this.maxWrite, data.Length - written);
            byte[] command = [0x00, 0xD6, (byte)(at >> 8), (byte)at, (byte)length, .. data.AsSpan(written, length)];

            var response = await this.channel.Exchange(command, ct).ConfigureAwait(false);
            if (response.StatusWord == SecurityStatusNotSatisfied)
                throw new NfcTagReadOnlyException();
            if (!response.IsSuccess)
                throw new NfcException($"Writing the NDEF file at {at} failed - the tag answered status {response.StatusWord:X4}");

            written += length;
        }
    }
}
