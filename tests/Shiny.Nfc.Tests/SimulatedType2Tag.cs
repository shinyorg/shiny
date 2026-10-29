using Shiny.Nfc.Infrastructure.Tags;

namespace Shiny.Nfc.Tests;


/// <summary>
/// An NFC Forum Type 2 tag behind a PC/SC reader: answers FF CA (UID), FF B0 (read pages) and
/// FF D6 (write a page), and enforces the hardware's write rules - read-only UID pages, one-time
/// programmable capability container and lock bytes, and pages locked by the static and dynamic
/// lock bits.
/// </summary>
class SimulatedType2Tag : IApduChannel
{
    public static readonly byte[] Uid = [0x04, 0xA1, 0xB2, 0xC3, 0xD4, 0xE5, 0xF6];

    /// <summary>The ATR an ACS reader synthesises for an Ultralight/NTAG.</summary>
    public static readonly byte[] UltralightAtr = Convert.FromHexString("3B8F8001804F0CA0000003060300030000000068");

    readonly int? dynamicLockPage;
    readonly int pagesPerDynamicLockBit;

    public byte[] Memory { get; }
    public List<(int Page, byte[] Data)> Writes { get; } = new();
    public bool TagRemoved { get; set; }


    SimulatedType2Tag(int pages, byte[] cc, byte[] data, int? dynamicLockPage, int pagesPerDynamicLockBit)
    {
        this.Memory = new byte[pages * 4];
        this.dynamicLockPage = dynamicLockPage;
        this.pagesPerDynamicLockBit = pagesPerDynamicLockBit;

        Uid[..3].CopyTo(this.Memory, 0);
        this.Memory[3] = 0x88;                               // BCC0
        Uid[3..].CopyTo(this.Memory, 4);
        this.Memory[8] = 0x44;                               // BCC1
        this.Memory[9] = 0x48;                               // internal
        cc.CopyTo(this.Memory, 12);
        data.CopyTo(this.Memory, 16);
    }


    /// <summary>NTAG213 as it leaves the factory: 144-byte data area, lock control TLV, empty NDEF TLV, dynamic lock bytes at page 40.</summary>
    public static SimulatedType2Tag Ntag213() => new(
        45,
        [0xE1, 0x10, 0x12, 0x00],
        [0x01, 0x03, 0xA0, 0x0C, 0x34, 0x03, 0x00, 0xFE],
        dynamicLockPage: 40,
        pagesPerDynamicLockBit: 2
    );


    /// <summary>NTAG215 as it leaves the factory: 496-byte data area, no lock control TLV.</summary>
    public static SimulatedType2Tag Ntag215() => new(
        135,
        [0xE1, 0x10, 0x3E, 0x00],
        [0x03, 0x00, 0xFE],
        dynamicLockPage: null,
        pagesPerDynamicLockBit: 0
    );


    public static SimulatedType2Tag Custom(byte[] cc, byte[] data, int pages = 45) => new(pages, cc, data, null, 0);


    public int PageCount => this.Memory.Length / 4;
    public byte[] Page(int page) => this.Memory[(page * 4)..((page + 1) * 4)];
    public byte[] DataArea(int length) => this.Memory[16..(16 + length)];


    public ValueTask<byte[]> Transmit(byte[] command, CancellationToken ct)
    {
        if (this.TagRemoved)
            throw new NfcTagLostException();

        return ValueTask.FromResult(this.Handle(command));
    }


    byte[] Handle(byte[] c)
    {
        if (c is [0xFF, 0xCA, 0x00, 0x00, 0x00])
            return [.. Uid, 0x90, 0x00];

        if (c is [0xFF, 0xB0, 0x00, var readPage, var length] && readPage < this.PageCount)
        {
            // reads roll over past the last page, as NTAG does
            var result = new byte[length + 2];
            for (var i = 0; i < length; i++)
                result[i] = this.Memory[(readPage * 4 + i) % this.Memory.Length];

            result[^2] = 0x90;
            return result;
        }

        if (c.Length == 9 && c[0] == 0xFF && c[1] == 0xD6 && c[2] == 0x00 && c[4] == 0x04 && c[3] < this.PageCount)
        {
            var page = c[3];
            var data = c[5..9];
            if (!this.TryWrite(page, data))
                return [0x63, 0x00];

            this.Writes.Add((page, data));
            return [0x90, 0x00];
        }

        return [0x6A, 0x82];
    }


    bool TryWrite(int page, byte[] data)
    {
        if (page < 2 || this.IsLocked(page))
            return false;

        var at = page * 4;
        if (page == 2)
        {
            // serial number bytes are ignored, lock bytes are one-time programmable
            this.Memory[at + 2] |= data[2];
            this.Memory[at + 3] |= data[3];
        }
        else if (page == 3 || page == this.dynamicLockPage)
        {
            for (var i = 0; i < 4; i++)
                this.Memory[at + i] |= data[i];
        }
        else
        {
            data.CopyTo(this.Memory, at);
        }
        return true;
    }


    bool IsLocked(int page)
    {
        var lock0 = this.Memory[10];
        var lock1 = this.Memory[11];

        if (page >= 3 && page <= 7)
            return (lock0 & (1 << page)) != 0;

        if (page >= 8 && page <= 15)
            return (lock1 & (1 << (page - 8))) != 0;

        if (this.dynamicLockPage is { } lockPage && page >= 16 && page < lockPage)
        {
            var bit = (page - 16) / this.pagesPerDynamicLockBit;
            var lockBytes = this.Memory[(lockPage * 4)..(lockPage * 4 + 3)];
            return (lockBytes[bit / 8] & (1 << (bit % 8))) != 0;
        }
        return false;
    }
}
