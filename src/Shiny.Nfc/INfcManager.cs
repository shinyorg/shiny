namespace Shiny.Nfc;


/// <summary>
/// Reads and writes NDEF tags.
/// </summary>
/// <remarks>
/// <para>NDEF only, on purpose: URLs, text, MIME payloads, smart posters and app records cover
/// almost every NFC feature an app ships, and NDEF is the one thing every platform - including Web
/// NFC in the browser - can do. Payment cards, passports, MIFARE Classic and raw tag commands are
/// out of scope.</para>
/// <para><b>A scan is an <see cref="IAsyncEnumerable{T}"/>, and the scan session lives exactly as
/// long as the enumeration.</b> The radio starts on the first <c>MoveNextAsync</c>; leaving the
/// <c>await foreach</c> - <c>break</c>, cancellation or an exception - ends it, which dismisses the
/// iOS scan sheet and stops Android reader mode. A tag is only usable inside the loop body that
/// received it.</para>
/// </remarks>
public interface INfcManager
{
    /// <summary>
    /// What this platform and device can do. <see cref="NfcCapabilities.None"/> when there is no
    /// NFC hardware or no backend for the platform.
    /// </summary>
    NfcCapabilities Capabilities { get; }

    /// <summary>
    /// Checks that NFC is present and switched on, asking for permission where the platform has
    /// one to ask for.
    /// </summary>
    Task<AccessState> RequestAccess(CancellationToken ct = default);

    /// <summary>
    /// Starts a scan session and yields each tag that comes into range.
    /// </summary>
    /// <remarks>
    /// <para>Only one scan can run at a time - no platform allows two sessions - so a second
    /// concurrent enumeration throws <see cref="InvalidOperationException"/> on its first
    /// <c>MoveNextAsync</c>.</para>
    /// <para>While the loop body is busy with one tag, at most one newer tag is held for it; an
    /// older unhandled tag is dropped in favour of the newer one, since it has most likely left
    /// the field already. Calls on a tag that has left the field throw
    /// <see cref="NfcTagLostException"/>.</para>
    /// <para>The enumeration ends normally when the user dismisses the platform's scan UI, and
    /// throws <see cref="NfcException"/> when the platform ends the session for any other
    /// reason.</para>
    /// </remarks>
    IAsyncEnumerable<INfcTag> Scan(NfcScanOptions? options = null, CancellationToken ct = default);
}
