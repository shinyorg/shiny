using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Nfc.Infrastructure;

namespace Shiny.Nfc;


/// <summary>
/// NDEF through the browser's Web NFC API (<c>NDEFReader</c>) - Chrome on Android, in a secure
/// context.
/// </summary>
/// <remarks>
/// <para>The first scan shows the browser's NFC permission prompt and has to start from a user
/// gesture (a button click). There is no system scan UI: the scan runs until the loop exits.</para>
/// <para>Web NFC delivers the message with the tap, so <see cref="INfcTag.ReadNdef"/> returns what
/// was read rather than reading again. A write goes to the next tag in range - the one just read,
/// as long as it is still held there. It does not report whether a tag is locked or how much it
/// holds, so <see cref="INfcTag.IsWritable"/> is optimistic and <see cref="INfcTag.MaxSize"/> is
/// null; a write that cannot succeed throws.</para>
/// </remarks>
class WebNfcManager(IJSRuntime js, ILogger<WebNfcManager> logger) : AbstractNfcManager(logger)
{
    IJSObjectReference? module;
    bool? supported;


    internal async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await js
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Nfc/nfc.js")
            .ConfigureAwait(false);

        return this.module;
    }


    // feature detection needs a JS round trip; RequestAccess and Scan both fill this in
    public override NfcCapabilities Capabilities => this.supported == false
        ? NfcCapabilities.None
        : NfcCapabilities.ReadNdef | NfcCapabilities.WriteNdef | NfcCapabilities.MakeReadOnly | NfcCapabilities.TagIdentifier;


    public override async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var m = await this.GetModule().ConfigureAwait(false);
        var state = await m.InvokeAsync<string>("permission", ct).ConfigureAwait(false);
        this.supported = state != "unsupported";

        return state switch
        {
            "unsupported" => AccessState.NotSupported,
            "insecure" => AccessState.Restricted,
            "granted" => AccessState.Available,
            "denied" => AccessState.Denied,
            _ => AccessState.Unknown // "prompt" - the browser asks on the first scan
        };
    }


    protected override async Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
    {
        var m = await this.GetModule().ConfigureAwait(false);
        var session = new ScanSession(m, sink);
        try
        {
            await m.InvokeVoidAsync("start", ct, session.Reference).ConfigureAwait(false);
            this.supported = true;
        }
        catch (JSException ex)
        {
            session.Reference.Dispose();
            if (ex.Message.Contains("NDEFReader", StringComparison.Ordinal) && ex.Message.Contains("unsupported", StringComparison.OrdinalIgnoreCase))
            {
                this.supported = false;
                throw new NfcNotSupportedException("This browser does not support Web NFC - it needs Chrome on Android", NfcCapabilities.ReadNdef);
            }
            throw new NfcException($"Web NFC could not start: {ex.Message}", ex);
        }
        return session;
    }


    internal sealed class ScanSession : IAsyncDisposable
    {
        readonly IJSObjectReference module;
        readonly NfcScanSink sink;
        int disposed;


        public ScanSession(IJSObjectReference module, NfcScanSink sink)
        {
            this.module = module;
            this.sink = sink;
            this.Reference = DotNetObjectReference.Create(this);
        }


        public DotNetObjectReference<ScanSession> Reference { get; }


        /// <summary>Called from JavaScript for each tag read.</summary>
        [JSInvokable]
        public void OnReading(string? serialNumber, string recordsJson)
        {
            try
            {
                var message = WebNfcMapper.FromJson(recordsJson);
                this.sink.Report(new WebNfcTag(this.module, ParseSerial(serialNumber), message));
            }
            catch (Exception ex) when (ex is NfcException or ArgumentException or FormatException or System.Text.Json.JsonException)
            {
                // a tag whose message does not survive the trip still came into range; it reads as invalid
                this.sink.Report(new WebNfcTag(this.module, ParseSerial(serialNumber), null, new NdefFormatException($"The tag holds an invalid NDEF message: {ex.Message}")));
            }
        }


        /// <summary>Called from JavaScript when a tag came into range but could not be read as NDEF.</summary>
        [JSInvokable]
        public void OnReadingError() => this.sink.Report(new WebNfcTag(this.module, null, null));


        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) != 0)
                return;

            try
            {
                await this.module.InvokeVoidAsync("stop").ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // the page is going away - nothing left to stop
            }
            this.Reference.Dispose();
            this.sink.Complete();
        }


        // "04:a2:5c:..." - null for tags (phones in emulation, mostly) that report none
        static byte[]? ParseSerial(string? serial)
        {
            if (String.IsNullOrWhiteSpace(serial))
                return null;

            try
            {
                return Convert.FromHexString(serial.Replace(":", ""));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}


/// <summary>A tag delivered by Web NFC's <c>reading</c> event.</summary>
sealed class WebNfcTag(IJSObjectReference module, byte[]? identifier, NdefMessage? message, NdefFormatException? readError = null) : INfcTag
{
    public byte[]? Identifier => identifier;
    public bool IsWritable { get; private set; } = true;
    public int? MaxSize => null;


    public Task<NdefMessage?> ReadNdef(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return readError == null ? Task.FromResult(message) : Task.FromException<NdefMessage?>(readError);
    }


    public async Task WriteNdef(NdefMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var json = WebNfcMapper.ToJson(message);
        await Call("write", ct, json).ConfigureAwait(false);
    }


    public async Task MakeReadOnly(CancellationToken ct = default)
    {
        await Call("makeReadOnly", ct).ConfigureAwait(false);
        this.IsWritable = false;
    }


    async Task Call(string identifier, CancellationToken ct, params object[] args)
    {
        try
        {
            await module.InvokeVoidAsync(identifier, ct, args).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw ex.Message switch
            {
                var m when m.Contains("NotAllowedError", StringComparison.Ordinal) && m.Contains("read-only", StringComparison.OrdinalIgnoreCase) => new NfcTagReadOnlyException(),
                var m when m.Contains("NetworkError", StringComparison.Ordinal) => new NfcTagLostException(ex),
                var m when m.Contains("NotSupportedError", StringComparison.Ordinal) => new NfcNotSupportedException($"Web NFC cannot do this: {ex.Message}", NfcCapabilities.WriteNdef),
                _ => new NfcException($"Web NFC failed: {ex.Message}", ex)
            };
        }
    }
}
