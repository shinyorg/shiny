using Microsoft.Extensions.Logging;
using Shiny.Nfc.Infrastructure;
using Shiny.Nfc.Infrastructure.Tags;
using Shiny.Nfc.Pcsc;

namespace Shiny.Nfc;


/// <summary>
/// NFC through a PC/SC contactless reader - Windows (<c>winscard.dll</c>), macOS
/// (<c>PCSC.framework</c>) and Linux (<c>pcsc-lite</c>). PC/SC gives raw tag access only, so NDEF is
/// spoken by this library: NFC Forum Type 2 (NTAG, MIFARE Ultralight) through the reader's storage
/// card commands, and Type 4 (ISO-DEP) through APDUs.
/// </summary>
/// <remarks>
/// <para>Every connected reader is watched, and readers plugged in or out during a scan are picked
/// up within a second. If PC/SC is not running yet - <c>pcscd</c> not started, or a Mac with no
/// reader attached - the scan keeps waiting for it rather than failing.</para>
/// <para>MIFARE Classic and other storage cards that are not Type 2 are reported, but read as
/// having no NDEF and refuse writes.</para>
/// </remarks>
class PcscNfcManager : AbstractNfcManager
{
    const uint PollMs = 1000;
    readonly IPcscApi? api;


    public PcscNfcManager(ILogger<PcscNfcManager> logger) : this(CreateApi(), logger) { }

    internal PcscNfcManager(IPcscApi? api, ILogger logger) : base(logger) => this.api = api;


    static IPcscApi? CreateApi()
    {
        if (OperatingSystem.IsWindows())
            return new WinScardApi();

        if (OperatingSystem.IsMacOS())
            return new MacPcscApi();

        if (OperatingSystem.IsLinux())
            return new PcscLiteApi();

        return null;
    }


    // a reader can be plugged in at any time, so the platform's PC/SC support is the capability, not
    // whether a reader happens to be attached right now
    public override NfcCapabilities Capabilities => this.api == null
        ? NfcCapabilities.None
        : NfcCapabilities.ReadNdef | NfcCapabilities.WriteNdef | NfcCapabilities.MakeReadOnly | NfcCapabilities.TagIdentifier;


    public override async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        if (this.api is not { } pcsc)
            return AccessState.NotSupported;

        return await Task.Run(() =>
        {
            nint context;
            try
            {
                context = pcsc.EstablishContext();
            }
            catch (PcscException)
            {
                return AccessState.NotSetup; // PC/SC service not running
            }
            catch (NfcNotSupportedException)
            {
                return AccessState.NotSupported; // pcsc-lite not installed
            }

            try
            {
                // no reader plugged in yet is "not set up", not "not supported"
                return pcsc.ListReaders(context).Length > 0 ? AccessState.Available : AccessState.NotSetup;
            }
            finally
            {
                pcsc.ReleaseContext(context);
            }
        }, ct).ConfigureAwait(false);
    }


    protected override Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
    {
        var pcsc = this.api ?? throw new NfcNotSupportedException("PC/SC is not available on this platform", NfcCapabilities.ReadNdef);
        var session = new ScanSession(pcsc, sink, this.Logger);
        session.Start();
        return Task.FromResult<IAsyncDisposable>(session);
    }


    sealed class ScanSession(IPcscApi api, NfcScanSink sink, ILogger logger) : IAsyncDisposable
    {
        readonly CancellationTokenSource stop = new();
        readonly Dictionary<string, uint> known = new();
        readonly Dictionary<string, PcscTag> present = new();
        nint context;
        Task? loop;


        public void Start() => this.loop = Task.Run(this.Run);


        async Task Run()
        {
            try
            {
                while (!this.stop.IsCancellationRequested)
                {
                    if (this.context != 0 || await this.TryEstablish().ConfigureAwait(false))
                        await this.Poll().ConfigureAwait(false);
                }
                sink.Complete();
            }
            catch (OperationCanceledException) when (this.stop.IsCancellationRequested)
            {
                sink.Complete();
            }
            catch (Exception ex)
            {
                sink.Fail(ex as NfcException ?? new NfcException($"The PC/SC scan failed: {ex.Message}", ex));
            }
            finally
            {
                this.DropAll();
                if (this.context != 0)
                    api.ReleaseContext(this.context);
            }
        }


        async Task<bool> TryEstablish()
        {
            try
            {
                this.context = await Task.Run(api.EstablishContext).ConfigureAwait(false);
                return true;
            }
            catch (PcscException ex) when (ex.Code is PcscError.NoService or PcscError.ServiceStopped)
            {
                // pcscd not up yet, or (macOS) no reader attached - wait for it
                await Task.Delay(TimeSpan.FromMilliseconds(PollMs), this.stop.Token).ConfigureAwait(false);
                return false;
            }
        }


        async Task Poll()
        {
            var readers = await Task.Run(() => api.ListReaders(this.context)).ConfigureAwait(false);
            this.Forget(readers);

            if (readers.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(PollMs), this.stop.Token).ConfigureAwait(false);
                return;
            }

            var states = readers
                .Select(r => new ReaderState(r, this.known.GetValueOrDefault(r, PcscState.Unaware)))
                .ToArray();

            var rc = await Task.Run(() => api.GetStatusChange(this.context, states, PollMs)).ConfigureAwait(false);
            switch (rc)
            {
                case PcscError.Success:
                    break;

                case PcscError.Timeout:
                case PcscError.Cancelled:
                    return;

                case PcscError.NoService:
                case PcscError.ServiceStopped:
                    // pcscd restarted, or the last reader went away on macOS - start over
                    this.DropAll();
                    api.ReleaseContext(this.context);
                    this.context = 0;
                    return;

                default:
                    if (PcscError.IsReaderGone(rc))
                        return; // re-listed on the next pass

                    throw new PcscException("SCardGetStatusChange", rc);
            }

            foreach (var state in states.Where(x => (x.EventState & PcscState.Changed) != 0))
            {
                var current = state.EventState & ~PcscState.Changed;
                this.known[state.Reader] = current;

                var hasTag = (current & PcscState.Present) != 0 && (current & PcscState.Mute) == 0;
                if (!hasTag)
                    this.Drop(state.Reader);
                else if (!this.present.ContainsKey(state.Reader))
                    await this.Connect(state.Reader, state.Atr).ConfigureAwait(false);
            }
        }


        async Task Connect(string reader, byte[] atr)
        {
            nint card = 0;
            try
            {
                var (handle, protocol) = await Task.Run(() => api.Connect(this.context, reader)).ConfigureAwait(false);
                card = handle;

                var channel = new PcscChannel(api, card, protocol);
                var tag = await ApduNfcTag.Connect(channel, atr, logger, this.stop.Token).ConfigureAwait(false);

                var pcscTag = new PcscTag(tag, channel);
                this.present[reader] = pcscTag;
                sink.Report(pcscTag);
            }
            catch (Exception ex) when (ex is PcscException or NfcException && !this.stop.IsCancellationRequested)
            {
                // pulled away mid-connect, or not a tag we can open - wait for the next tap
                logger.LogDebug(ex, "Could not open the tag on {Reader}", reader);
                if (card != 0 && !this.present.ContainsKey(reader))
                    api.Disconnect(card);
            }
        }


        // readers unplugged since the last pass
        void Forget(string[] readers)
        {
            foreach (var gone in this.known.Keys.Except(readers).ToList())
            {
                this.known.Remove(gone);
                this.Drop(gone);
            }
        }


        void Drop(string reader)
        {
            if (this.present.Remove(reader, out var tag))
                tag.Dispose();
        }


        void DropAll()
        {
            foreach (var tag in this.present.Values)
                tag.Dispose();

            this.present.Clear();
            this.known.Clear();
        }


        public async ValueTask DisposeAsync()
        {
            if (this.stop.IsCancellationRequested)
                return;

            this.stop.Cancel();
            if (this.context != 0)
                api.Cancel(this.context); // wakes a blocked SCardGetStatusChange

            if (this.loop != null)
                await this.loop.ConfigureAwait(false);

            this.stop.Dispose();
        }
    }
}


/// <summary>APDUs over one PC/SC card handle.</summary>
sealed class PcscChannel(IPcscApi api, nint card, uint protocol) : IApduChannel, IDisposable
{
    int disposed;


    public async ValueTask<byte[]> Transmit(byte[] command, CancellationToken ct)
    {
        if (Volatile.Read(ref this.disposed) != 0)
            throw new NfcTagLostException();

        try
        {
            return await Task.Run(() => api.Transmit(card, protocol, command), ct).ConfigureAwait(false);
        }
        catch (PcscException ex) when (PcscError.IsCardGone(ex.Code) || PcscError.IsReaderGone(ex.Code))
        {
            throw new NfcTagLostException(ex);
        }
        catch (PcscException ex)
        {
            throw new NfcException($"Talking to the tag failed: {ex.Message}", ex);
        }
    }


    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            api.Disconnect(card);
    }
}


/// <summary>A tag on a PC/SC reader. Disposed - its card handle closed - once it leaves the reader.</summary>
sealed class PcscTag(ApduNfcTag tag, PcscChannel channel) : INfcTag, IDisposable
{
    public byte[]? Identifier => tag.Identifier;
    public bool IsWritable => tag.IsWritable;
    public int? MaxSize => tag.MaxSize;

    public Task<NdefMessage?> ReadNdef(CancellationToken ct = default) => tag.ReadNdef(ct);
    public Task WriteNdef(NdefMessage message, CancellationToken ct = default) => tag.WriteNdef(message, ct);
    public Task MakeReadOnly(CancellationToken ct = default) => tag.MakeReadOnly(ct);

    public void Dispose() => channel.Dispose();
}
