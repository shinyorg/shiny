namespace Shiny.Printers;


/// <summary>
/// An <see cref="IPrinterConnection"/> that can also read the printer's replies - needed for status queries.
/// TCP has a back-channel; write-only transports (most BLE printers, the browser transports) do not.
/// </summary>
public interface IPrinterQueryConnection : IPrinterConnection
{
    /// <summary>
    /// Discards any unread bytes, writes <paramref name="request"/>, then reads exactly
    /// <paramref name="responseLength"/> bytes. Never interleaves with <see cref="IPrinterConnection.SendAsync"/>.
    /// </summary>
    ValueTask<byte[]> Query(ReadOnlyMemory<byte> request, int responseLength, CancellationToken cancellationToken = default);
}
