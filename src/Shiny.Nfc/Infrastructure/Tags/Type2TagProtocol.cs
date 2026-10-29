using Microsoft.Extensions.Logging;

namespace Shiny.Nfc.Infrastructure.Tags;


/// <summary>
/// NFC Forum Type 2 Tag (NTAG21x, MIFARE Ultralight) over PC/SC storage-card pseudo-APDUs.
/// </summary>
/// <remarks>
/// <para>Memory is 4-byte pages. Pages 0-2 hold the UID and static lock bytes, page 3 the capability
/// container (<c>E1 10 &lt;size/8&gt; &lt;access&gt;</c>), and the data area starts at page 4 as a
/// TLV stream: lock control (01), memory control (02), the NDEF message (03) and a terminator
/// (FE).</para>
/// <para>Writes follow the specification's torn-write order: the NDEF TLV is first written with
/// length 0, then the message, then the real length - so a tag pulled away mid-write reads as empty
/// rather than as a half-written message.</para>
/// <para>Not handled: reserved memory areas a lock or memory control TLV places inside the data area.
/// NTAG21x and Ultralight keep their lock bytes after the data area, so this matters only for
/// larger, rarer Type 2 tags.</para>
/// </remarks>
sealed class Type2TagProtocol : INdefTagProtocol
{
    const int PageSize = 4;
    const int DataStartPage = 4;
    const int MaxReadBytes = 16;

    const byte TlvNull = 0x00;
    const byte TlvLockControl = 0x01;
    const byte TlvNdef = 0x03;
    const byte TlvTerminator = 0xFE;

    readonly IApduChannel channel;
    readonly ILogger logger;
    readonly byte[] cc;
    readonly int dataSize;
    readonly Dictionary<int, byte[]> dataPageCache = new();

    int ndefOffset;         // data-area offset of the NDEF TLV, or where one would be written
    int ndefLength;         // 0 when there is no message
    int ndefHeaderLength;
    byte[]? lockControl;


    Type2TagProtocol(IApduChannel channel, ILogger logger, byte[] cc)
    {
        this.channel = channel;
        this.logger = logger;
        this.cc = cc;
        this.dataSize = cc[2] * 8;
    }


    public static async Task<Type2TagProtocol> Open(IApduChannel channel, ILogger logger, CancellationToken ct)
    {
        var header = await ReadPages(channel, 0, MaxReadBytes, ct).ConfigureAwait(false);
        var protocol = new Type2TagProtocol(channel, logger, header[12..16]);

        if (protocol.IsNdefFormatted)
            await protocol.ScanTlvs(ct).ConfigureAwait(false);

        return protocol;
    }


    public bool IsNdefFormatted => this.cc[0] == 0xE1 && (this.cc[1] >> 4) == 1 && this.dataSize > 0;
    public bool IsWritable => this.IsNdefFormatted && this.cc[3] == 0x00;

    public int? MaxSize
    {
        get
        {
            if (!this.IsNdefFormatted)
                return null;

            var available = this.dataSize - this.ndefOffset;
            if (available - 2 <= 0xFE)
                return Math.Max(0, available - 2);

            return Math.Min(available - 4, 0xFFFE);
        }
    }


    public async Task<NdefMessage?> ReadNdef(CancellationToken ct)
    {
        if (!this.IsNdefFormatted || this.ndefLength == 0)
            return null;

        var start = this.ndefOffset + this.ndefHeaderLength;
        if (start + this.ndefLength > this.dataSize)
            throw new NdefFormatException($"The NDEF TLV claims {this.ndefLength} bytes, which runs past the end of the tag's {this.dataSize}-byte data area");

        var bytes = await this.ReadData(start, this.ndefLength, ct).ConfigureAwait(false);
        return NdefMessage.Parse(bytes);
    }


    public async Task WriteNdef(NdefMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!this.IsNdefFormatted)
            throw new NfcException("The tag is not NDEF formatted");
        if (!this.IsWritable)
            throw new NfcTagReadOnlyException();

        var encoded = message.ToByteArray();
        var max = this.MaxSize!.Value;
        if (encoded.Length > max)
            throw new NdefMessageTooLargeException(encoded.Length, max);

        var headerLength = encoded.Length < 0xFF ? 2 : 4;
        var tlvLength = headerLength + encoded.Length;
        var terminator = this.ndefOffset + tlvLength < this.dataSize;

        var firstPage = this.ndefOffset / PageSize;
        var lastPage = (this.ndefOffset + tlvLength + (terminator ? 1 : 0) - 1) / PageSize;
        var headerLastPage = (this.ndefOffset + headerLength - 1) / PageSize;

        // the first page may start with TLVs that stay - lock control on an NTAG213, for one
        var final = new byte[(lastPage - firstPage + 1) * PageSize];
        var existing = await this.ReadData(firstPage * PageSize, PageSize, ct).ConfigureAwait(false);
        existing.CopyTo(final, 0);

        var at = this.ndefOffset - firstPage * PageSize;
        final[at] = TlvNdef;
        if (headerLength == 2)
        {
            final[at + 1] = (byte)encoded.Length;
        }
        else
        {
            final[at + 1] = 0xFF;
            final[at + 2] = (byte)(encoded.Length >> 8);
            final[at + 3] = (byte)encoded.Length;
        }
        encoded.CopyTo(final, at + headerLength);
        if (terminator)
            final[at + tlvLength] = TlvTerminator;

        // same TLV, length 0 - in the 3-byte form when that is what the final length needs, so the value does not move
        var empty = (byte[])final.Clone();
        for (var i = 1; i < headerLength; i++)
            empty[at + i] = headerLength == 4 && i == 1 ? (byte)0xFF : (byte)0x00;

        this.dataPageCache.Clear();
        this.ndefLength = 0;

        for (var page = firstPage; page <= headerLastPage; page++)
            await this.WriteDataPage(page, empty, (page - firstPage) * PageSize, ct).ConfigureAwait(false);

        for (var page = headerLastPage + 1; page <= lastPage; page++)
            await this.WriteDataPage(page, final, (page - firstPage) * PageSize, ct).ConfigureAwait(false);

        for (var page = firstPage; page <= headerLastPage; page++)
            await this.WriteDataPage(page, final, (page - firstPage) * PageSize, ct).ConfigureAwait(false);

        this.ndefLength = encoded.Length;
        this.ndefHeaderLength = headerLength;
    }


    public async Task MakeReadOnly(CancellationToken ct)
    {
        if (!this.IsNdefFormatted)
            throw new NfcException("The tag is not NDEF formatted");

        // capability container first - the static lock bits written last can lock its page
        await WritePage(this.channel, 3, [this.cc[0], this.cc[1], this.cc[2], 0x0F], ct).ConfigureAwait(false);
        this.cc[3] = 0x0F;

        if (this.lockControl == null)
        {
            this.logger.NoDynamicLockBytes();
        }
        else
        {
            // lock control TLV value: [page address | byte offset] [lock bit count] [bytes locked per bit | bytes per page], log2 nibbles
            var pageAddress = this.lockControl[0] >> 4;
            var byteOffset = this.lockControl[0] & 0x0F;
            var bits = this.lockControl[1] == 0 ? 256 : this.lockControl[1];
            var bytesPerPage = 1 << (this.lockControl[2] & 0x0F);

            var lockBytes = new byte[(bits + 7) / 8];
            Array.Fill(lockBytes, (byte)0xFF);
            if (bits % 8 != 0)
                lockBytes[^1] = (byte)((1 << (bits % 8)) - 1);

            await this.SetBits(pageAddress * bytesPerPage + byteOffset, lockBytes, ct).ConfigureAwait(false);
        }

        // static lock bytes - page 2, bytes 2 and 3
        await this.SetBits(2 * PageSize + 2, [0xFF, 0xFF], ct).ConfigureAwait(false);
        this.dataPageCache.Clear();
    }


    async Task ScanTlvs(CancellationToken ct)
    {
        var pos = 0;
        while (pos < this.dataSize)
        {
            var type = await this.ByteAt(pos, ct).ConfigureAwait(false);
            if (type == TlvNull)
            {
                pos++;
                continue;
            }
            if (type == TlvTerminator || pos + 1 >= this.dataSize)
                break;

            int length = await this.ByteAt(pos + 1, ct).ConfigureAwait(false);
            var headerLength = 2;
            if (length == 0xFF)
            {
                if (pos + 3 >= this.dataSize)
                    break;

                length = (await this.ByteAt(pos + 2, ct).ConfigureAwait(false) << 8) | await this.ByteAt(pos + 3, ct).ConfigureAwait(false);
                headerLength = 4;
            }

            if (type == TlvNdef)
            {
                this.ndefOffset = pos;
                this.ndefLength = length;
                this.ndefHeaderLength = headerLength;
                return;
            }
            if (type == TlvLockControl && length == 3 && pos + 5 <= this.dataSize)
                this.lockControl = await this.ReadData(pos + 2, 3, ct).ConfigureAwait(false);

            pos += headerLength + length;
        }

        // no NDEF TLV - a new one goes where the terminator (or the end of the TLVs) is
        this.ndefOffset = Math.Min(pos, this.dataSize);
        this.ndefLength = 0;
        this.ndefHeaderLength = 0;
    }


    async ValueTask<byte> ByteAt(int offset, CancellationToken ct)
        => (await this.ReadData(offset, 1, ct).ConfigureAwait(false))[0];


    /// <summary>Reads from the data area, offset 0 being page 4, through a page cache.</summary>
    async Task<byte[]> ReadData(int offset, int count, CancellationToken ct)
    {
        var result = new byte[count];
        var dataPages = this.dataSize / PageSize;

        for (var i = 0; i < count;)
        {
            var page = (offset + i) / PageSize;
            if (!this.dataPageCache.TryGetValue(page, out var bytes))
            {
                var readLength = Math.Min(MaxReadBytes, Math.Max(1, dataPages - page) * PageSize);
                var block = await ReadPages(this.channel, DataStartPage + page, readLength, ct).ConfigureAwait(false);
                for (var p = 0; p < readLength / PageSize; p++)
                    this.dataPageCache[page + p] = block[(p * PageSize)..((p + 1) * PageSize)];

                bytes = this.dataPageCache[page];
            }

            var inPage = (offset + i) % PageSize;
            var take = Math.Min(PageSize - inPage, count - i);
            Array.Copy(bytes, inPage, result, i, take);
            i += take;
        }
        return result;
    }


    Task WriteDataPage(int dataPage, byte[] source, int sourceOffset, CancellationToken ct)
        => WritePage(this.channel, DataStartPage + dataPage, source[sourceOffset..(sourceOffset + PageSize)], ct);


    /// <summary>ORs bits into memory at an absolute byte address - lock bits only ever go from 0 to 1.</summary>
    async Task SetBits(int address, byte[] bits, CancellationToken ct)
    {
        var firstPage = address / PageSize;
        var lastPage = (address + bits.Length - 1) / PageSize;

        for (var page = firstPage; page <= lastPage; page++)
        {
            var current = await ReadPages(this.channel, page, PageSize, ct).ConfigureAwait(false);
            for (var i = 0; i < PageSize; i++)
            {
                var index = page * PageSize + i - address;
                if (index >= 0 && index < bits.Length)
                    current[i] |= bits[index];
            }
            await WritePage(this.channel, page, current, ct).ConfigureAwait(false);
        }
    }


    static async Task<byte[]> ReadPages(IApduChannel channel, int page, int length, CancellationToken ct)
    {
        // PC/SC Part 3 READ BINARY: FF B0 00 <block> <Le>
        var data = await channel.Send([0xFF, 0xB0, 0x00, (byte)page, (byte)length], $"Reading page {page}", ct).ConfigureAwait(false);
        if (data.Length < length)
            throw new NfcException($"Reading page {page} returned {data.Length} bytes, expected {length}");

        return data[..length];
    }


    static async Task WritePage(IApduChannel channel, int page, byte[] data, CancellationToken ct)
    {
        // PC/SC Part 3 UPDATE BINARY: FF D6 00 <block> 04 <4 bytes>
        var response = await channel.Exchange([0xFF, 0xD6, 0x00, (byte)page, PageSize, .. data], ct).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw new NfcException($"Writing page {page} failed - the reader answered status {response.StatusWord:X4}. The page may be locked.");
    }
}
