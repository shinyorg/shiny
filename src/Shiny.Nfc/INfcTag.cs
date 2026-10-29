namespace Shiny.Nfc;


/// <summary>
/// A tag in range of the reader, handed out by <see cref="INfcManager.Scan"/>.
/// </summary>
/// <remarks>
/// Only valid inside the <c>await foreach</c> body that received it, and only while the tag stays
/// in the field. Do not keep a reference to it - keep its <see cref="Identifier"/> or the message
/// you read from it instead.
/// </remarks>
public interface INfcTag
{
    /// <summary>
    /// The tag's UID, or null where the platform does not expose it.
    /// </summary>
    /// <remarks>
    /// Usually 4, 7 or 10 bytes. Not a secret and not unique across manufacturers in any guaranteed
    /// way - fine for "which sticker is this", not for authentication. Some phones acting as tags
    /// randomise it on every tap.
    /// </remarks>
    byte[]? Identifier { get; }

    /// <summary>
    /// True when the tag is NDEF formatted and not locked against writing.
    /// </summary>
    bool IsWritable { get; }

    /// <summary>
    /// The largest NDEF message, in encoded bytes, the tag can hold, or null when it is unknown or
    /// the tag is not NDEF formatted.
    /// </summary>
    int? MaxSize { get; }

    /// <summary>
    /// Reads the tag's NDEF message, or null when the tag holds none - blank, freshly formatted, or
    /// not NDEF formatted at all.
    /// </summary>
    /// <exception cref="NdefFormatException">The tag holds bytes that are not a valid NDEF message.</exception>
    /// <exception cref="NfcTagLostException">The tag left the field.</exception>
    Task<NdefMessage?> ReadNdef(CancellationToken ct = default);

    /// <summary>
    /// Replaces the tag's NDEF message.
    /// </summary>
    /// <exception cref="NfcTagReadOnlyException">The tag is locked against writing.</exception>
    /// <exception cref="NdefMessageTooLargeException">The message does not fit; nothing was written.</exception>
    /// <exception cref="NfcTagLostException">The tag left the field. The previous message may be gone.</exception>
    Task WriteNdef(NdefMessage message, CancellationToken ct = default);

    /// <summary>
    /// Locks the tag so it can never be written again. <b>This is permanent</b> - there is no
    /// unlock, on any tag, from any app.
    /// </summary>
    /// <exception cref="NfcNotSupportedException">The tag or platform cannot be locked from here.</exception>
    Task MakeReadOnly(CancellationToken ct = default);
}
