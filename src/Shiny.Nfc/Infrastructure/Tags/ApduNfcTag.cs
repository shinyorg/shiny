using Microsoft.Extensions.Logging;

namespace Shiny.Nfc.Infrastructure.Tags;


/// <summary>
/// An <see cref="INfcTag"/> for backends that only give raw tag access - PC/SC on Windows, macOS
/// and Linux. Picks the NFC Forum tag type from the reader's ATR and speaks NDEF itself.
/// </summary>
sealed class ApduNfcTag(byte[]? identifier, INdefTagProtocol protocol) : INfcTag
{
    public static async Task<ApduNfcTag> Connect(IApduChannel channel, ReadOnlyMemory<byte> atr, ILogger logger, CancellationToken ct)
    {
        var identifier = await TryGetUid(channel, ct).ConfigureAwait(false);
        var (family, description) = Atr.Classify(atr.Span);

        INdefTagProtocol protocol = family switch
        {
            TagFamily.Type2 => await Type2TagProtocol.Open(channel, logger, ct).ConfigureAwait(false),
            TagFamily.IsoDep => await Type4TagProtocol.Open(channel, ct).ConfigureAwait(false),
            _ => new UnsupportedTagProtocol(description)
        };
        return new(identifier, protocol);
    }


    public byte[]? Identifier => identifier;
    public bool IsWritable => protocol.IsWritable;
    public int? MaxSize => protocol.MaxSize;

    public Task<NdefMessage?> ReadNdef(CancellationToken ct = default) => protocol.ReadNdef(ct);
    public Task WriteNdef(NdefMessage message, CancellationToken ct = default) => protocol.WriteNdef(message, ct);
    public Task MakeReadOnly(CancellationToken ct = default) => protocol.MakeReadOnly(ct);


    static async Task<byte[]?> TryGetUid(IApduChannel channel, CancellationToken ct)
    {
        // PC/SC Part 3 GET DATA: FF CA 00 00 00
        var response = await channel.Exchange([0xFF, 0xCA, 0x00, 0x00, 0x00], ct).ConfigureAwait(false);
        return response.IsSuccess && response.Data.Length > 0 ? response.Data : null;
    }
}


enum TagFamily
{
    Type2,
    IsoDep,
    Unsupported
}


/// <summary>
/// Reads what kind of contactless card a PC/SC reader found from the ATR it synthesises (PC/SC
/// Part 3, section 3.1.3.2.3).
/// </summary>
static class Atr
{
    /// <summary>
    /// A storage card's ATR: <c>3B 8F 80 01 80 4F 0C A0 00 00 03 06 SS NN NN 00 00 00 00 TCK</c>,
    /// where SS is the standard and NN NN the card name. Anything else is an ISO 14443-4 card,
    /// whose ATR carries its own historical bytes.
    /// </summary>
    public static (TagFamily Family, string Description) Classify(ReadOnlySpan<byte> atr)
    {
        ReadOnlySpan<byte> storageRid = [0xA0, 0x00, 0x00, 0x03, 0x06];

        if (atr.Length < 15 || atr[0] != 0x3B || atr[4] != 0x80 || atr[5] != 0x4F || !atr.Slice(7, 5).SequenceEqual(storageRid))
            return (TagFamily.IsoDep, "an ISO-DEP tag");

        var name = (atr[13] << 8) | atr[14];
        return name switch
        {
            0x0003 => (TagFamily.Type2, "a MIFARE Ultralight / NTAG tag"),
            0x003A => (TagFamily.Type2, "a MIFARE Ultralight C tag"),
            0x0001 or 0x0002 or 0x0026 => (TagFamily.Unsupported, "MIFARE Classic"),
            _ => (TagFamily.Unsupported, $"this storage card (PC/SC card name {name:X4})")
        };
    }
}
