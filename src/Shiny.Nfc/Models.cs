namespace Shiny.Nfc;


/// <summary>
/// What a platform's NFC backend can do.
/// </summary>
[Flags]
public enum NfcCapabilities
{
    /// <summary>No NFC - no hardware, NFC switched off at the hardware level, or no backend for this platform.</summary>
    None = 0,

    /// <summary>NDEF messages can be read.</summary>
    ReadNdef = 1,

    /// <summary>NDEF messages can be written.</summary>
    WriteNdef = 2,

    /// <summary><see cref="INfcTag.MakeReadOnly"/> works, for tags that support it.</summary>
    MakeReadOnly = 4,

    /// <summary><see cref="INfcTag.Identifier"/> is populated.</summary>
    TagIdentifier = 8
}


/// <summary>
/// Options for <see cref="INfcManager.Scan"/>.
/// </summary>
public class NfcScanOptions
{
    /// <summary>
    /// The prompt shown on the iOS scan sheet, such as "Hold your iPhone near the tag". Ignored on
    /// platforms without a system scan UI.
    /// </summary>
    public string? AlertMessage { get; set; }
}


/// <summary>
/// The result of <see cref="NfcManagerExtensions.ReadOne"/>.
/// </summary>
/// <param name="Identifier">The tag's UID, where the platform exposes it.</param>
/// <param name="Message">The tag's NDEF message, or null when it holds none.</param>
public record NfcReadResult(byte[]? Identifier, NdefMessage? Message);
