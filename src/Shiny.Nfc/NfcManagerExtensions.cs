namespace Shiny.Nfc;


/// <summary>
/// One-tap helpers over <see cref="INfcManager.Scan"/>.
/// </summary>
/// <remarks>
/// These do their work inside the scan loop and return the result, never the tag - the scan
/// session closes as the loop exits, and a tag cannot be read or written after that.
/// </remarks>
public static class NfcManagerExtensions
{
    /// <summary>
    /// Scans until one tag is tapped and reads it.
    /// </summary>
    /// <returns>The tag's identifier and NDEF message, or null if the scan ended first - the user dismissed the iOS scan sheet, for instance.</returns>
    public static async Task<NfcReadResult?> ReadOne(this INfcManager manager, NfcScanOptions? options = null, CancellationToken ct = default)
    {
        await foreach (var tag in manager.Scan(options, ct).ConfigureAwait(false))
        {
            var message = await tag.ReadNdef(ct).ConfigureAwait(false);
            return new(tag.Identifier, message);
        }
        return null;
    }


    /// <summary>
    /// Scans until one tag is tapped and writes a message to it.
    /// </summary>
    /// <returns>True once the message is written, false if the scan ended before a tag was tapped.</returns>
    public static async Task<bool> WriteOne(this INfcManager manager, NdefMessage message, NfcScanOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await foreach (var tag in manager.Scan(options, ct).ConfigureAwait(false))
        {
            await tag.WriteNdef(message, ct).ConfigureAwait(false);
            return true;
        }
        return false;
    }
}
