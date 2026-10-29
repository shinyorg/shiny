namespace Shiny.Nfc.Infrastructure.Tags;


/// <summary>
/// Sends ISO 7816-4 command APDUs to a tag and returns the response with its status word.
/// </summary>
/// <remarks>
/// PC/SC is the implementation: <c>SCardTransmit</c> for ISO 14443-4 tags, and the PC/SC Part 3
/// storage-card pseudo-APDUs (<c>FF B0</c> read, <c>FF D6</c> write, <c>FF CA</c> get UID) that the
/// reader translates for Type 2 tags. An implementation maps "card removed" to
/// <see cref="NfcTagLostException"/>.
/// </remarks>
interface IApduChannel
{
    ValueTask<byte[]> Transmit(byte[] command, CancellationToken ct);
}


/// <summary>A response APDU split into data and status word.</summary>
readonly record struct ApduResponse(byte[] Data, ushort StatusWord)
{
    public const ushort Success = 0x9000;

    public bool IsSuccess => this.StatusWord == Success;


    public static ApduResponse Parse(byte[] raw)
    {
        if (raw.Length < 2)
            throw new NfcException($"The tag returned a {raw.Length}-byte response - too short to hold a status word");

        return new(raw[..^2], (ushort)((raw[^2] << 8) | raw[^1]));
    }
}


static class ApduChannelExtensions
{
    public static async ValueTask<ApduResponse> Exchange(this IApduChannel channel, byte[] command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var raw = await channel.Transmit(command, ct).ConfigureAwait(false);
        return ApduResponse.Parse(raw);
    }


    /// <summary>Sends a command and returns its data, throwing when the status word is not 90 00.</summary>
    public static async ValueTask<byte[]> Send(this IApduChannel channel, byte[] command, string operation, CancellationToken ct)
    {
        var response = await channel.Exchange(command, ct).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw new NfcException($"{operation} failed - the tag answered status {response.StatusWord:X4}");

        return response.Data;
    }
}
