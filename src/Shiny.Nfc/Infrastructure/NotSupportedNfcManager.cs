using Microsoft.Extensions.Logging;

namespace Shiny.Nfc.Infrastructure;


/// <summary>
/// An <see cref="INfcManager"/> for hosts with no NFC - a server, a console tool, a test run, or a
/// platform whose backend has not landed yet.
/// </summary>
/// <remarks>
/// Reports <see cref="NfcCapabilities.None"/> and <see cref="AccessState.NotSupported"/>. Scanning
/// throws <see cref="NfcNotSupportedException"/> rather than waiting forever for a tag that cannot
/// arrive.
/// </remarks>
class NotSupportedNfcManager(ILogger<NotSupportedNfcManager> logger) : AbstractNfcManager(logger)
{
    public override NfcCapabilities Capabilities => NfcCapabilities.None;

    public override Task<AccessState> RequestAccess(CancellationToken ct = default)
        => Task.FromResult(AccessState.NotSupported);

    protected override Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
        => throw new NfcNotSupportedException("NFC is not supported on this platform", NfcCapabilities.ReadNdef);
}
