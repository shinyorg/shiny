namespace Shiny.Nfc.Infrastructure.Tags;


/// <summary>
/// NDEF over one NFC Forum tag type, spoken through an <see cref="IApduChannel"/>. Used where the
/// platform gives raw tag access but no NDEF support of its own - PC/SC on Windows, macOS and Linux.
/// </summary>
interface INdefTagProtocol
{
    bool IsNdefFormatted { get; }
    bool IsWritable { get; }
    int? MaxSize { get; }

    Task<NdefMessage?> ReadNdef(CancellationToken ct);
    Task WriteNdef(NdefMessage message, CancellationToken ct);
    Task MakeReadOnly(CancellationToken ct);
}


/// <summary>A tag this layer cannot speak NDEF to - MIFARE Classic, for one.</summary>
sealed class UnsupportedTagProtocol(string tagDescription) : INdefTagProtocol
{
    public bool IsNdefFormatted => false;
    public bool IsWritable => false;
    public int? MaxSize => null;

    public Task<NdefMessage?> ReadNdef(CancellationToken ct) => Task.FromResult<NdefMessage?>(null);

    public Task WriteNdef(NdefMessage message, CancellationToken ct)
        => throw new NfcNotSupportedException($"NDEF is not supported on {tagDescription}", NfcCapabilities.WriteNdef);

    public Task MakeReadOnly(CancellationToken ct)
        => throw new NfcNotSupportedException($"NDEF is not supported on {tagDescription}", NfcCapabilities.MakeReadOnly);
}
