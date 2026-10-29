using CoreFoundation;
using CoreNFC;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Nfc.Infrastructure;

namespace Shiny.Nfc;


/// <summary>
/// NDEF over CoreNFC's <see cref="NFCNdefReaderSession"/>: the system scan sheet stays up for the
/// whole scan, and each tag is connected and handed to the loop as it is tapped.
/// </summary>
/// <remarks>
/// <para>An NDEF session does not expose the tag's UID, so <see cref="INfcTag.Identifier"/> is
/// always null here.</para>
/// <para>iOS ends a session by itself after 60 seconds, or when the user taps Cancel; both end the
/// scan loop normally. Mac Catalyst compiles this backend, but CoreNFC has no reader on a Mac, so
/// it reports <see cref="NfcCapabilities.None"/> there.</para>
/// </remarks>
class AppleNfcManager(ILogger<AppleNfcManager> logger) : AbstractNfcManager(logger)
{
    public override NfcCapabilities Capabilities => NFCNdefReaderSession.ReadingAvailable
        ? NfcCapabilities.ReadNdef | NfcCapabilities.WriteNdef | NfcCapabilities.MakeReadOnly
        : NfcCapabilities.None;


    // CoreNFC has no permission prompt - the entitlement and NFCReaderUsageDescription are the permission
    public override Task<AccessState> RequestAccess(CancellationToken ct = default)
        => Task.FromResult(NFCNdefReaderSession.ReadingAvailable ? AccessState.Available : AccessState.NotSupported);


    protected override Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
    {
        if (!NFCNdefReaderSession.ReadingAvailable)
            throw new NfcNotSupportedException("This device cannot read NFC tags", NfcCapabilities.ReadNdef);

        var session = new ScanSession(sink);
        session.Begin(options.AlertMessage);
        return Task.FromResult<IAsyncDisposable>(session);
    }


    sealed class ScanSession(NfcScanSink sink) : NFCNdefReaderSessionDelegate, IAsyncDisposable
    {
        NFCNdefReaderSession? session;
        int ended;


        public void Begin(string? alertMessage)
        {
            this.session = new NFCNdefReaderSession(this, DispatchQueue.MainQueue, false);
            if (!String.IsNullOrWhiteSpace(alertMessage))
                this.session.AlertMessage = alertMessage;

            this.session.BeginSession();
        }


        // required by the protocol, but never called once DidDetectTags is implemented (iOS 13+)
        public override void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages) { }


        public override async void DidDetectTags(NFCNdefReaderSession session, INFCNdefTag[] tags)
        {
            try
            {
                if (tags.Length > 1)
                {
                    // CoreNFC can only connect to one - ask the user to present a single tag
                    session.AlertMessage = "More than one tag found - hold just one near the device";
                    await Task.Delay(500).ConfigureAwait(true);
                    session.RestartPolling();
                    return;
                }

                var tag = tags[0];
                await session.ConnectToTagAsync(tag).ConfigureAwait(true);
                var (status, capacity) = await AppleNfcTag.QueryStatus(tag).ConfigureAwait(true);

                sink.Report(new AppleNfcTag(tag, status, capacity));
            }
            catch (NSErrorException)
            {
                // the tag went before it could be connected - keep the sheet up for the next one
                if (Volatile.Read(ref this.ended) == 0)
                    session.RestartPolling();
            }
        }


        public override void DidInvalidate(NFCNdefReaderSession session, NSError error)
        {
            Interlocked.Exchange(ref this.ended, 1);
            switch ((NFCReaderError)(long)error.Code)
            {
                case NFCReaderError.ReaderSessionInvalidationErrorUserCanceled:
                case NFCReaderError.ReaderSessionInvalidationErrorSessionTimeout:
                case NFCReaderError.ReaderSessionInvalidationErrorFirstNDEFTagRead:
                    sink.Complete();
                    break;

                default:
                    sink.Fail(new NfcException($"The NFC session ended: {error.LocalizedDescription}"));
                    break;
            }
        }


        public ValueTask DisposeAsync()
        {
            // an invalidation from here reports UserCanceled through DidInvalidate, which completes the sink
            if (Interlocked.Exchange(ref this.ended, 1) == 0)
                this.session?.InvalidateSession();

            sink.Complete();
            return ValueTask.CompletedTask;
        }
    }
}
