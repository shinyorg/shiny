using Android.App;
using Android.Nfc;
using Microsoft.Extensions.Logging;
using Shiny.Nfc.Infrastructure;

namespace Shiny.Nfc;


/// <summary>
/// NDEF over Android reader mode: <c>NfcAdapter.enableReaderMode</c> on the current activity, so
/// tags come to the scan rather than launching the app through an intent filter.
/// </summary>
/// <remarks>
/// Reader mode belongs to a resumed activity and the system drops it when that activity pauses, so
/// the scan re-arms itself on whichever activity resumes next and keeps going across a screen
/// rotation or a trip to another activity. Tags arrive on a binder thread, and every tag call is
/// blocking I/O, so <see cref="AndroidNfcTag"/> runs them off the caller's thread.
/// </remarks>
class AndroidNfcManager(AndroidPlatform platform, ILogger<AndroidNfcManager> logger) : AbstractNfcManager(logger)
{
    const NfcReaderFlags ReaderFlags =
        NfcReaderFlags.NfcA |
        NfcReaderFlags.NfcB |
        NfcReaderFlags.NfcF |
        NfcReaderFlags.NfcV |
        NfcReaderFlags.NoPlatformSounds;

    NfcAdapter? Adapter => NfcAdapter.GetDefaultAdapter(platform.AppContext);


    public override NfcCapabilities Capabilities => this.Adapter == null
        ? NfcCapabilities.None
        : NfcCapabilities.ReadNdef | NfcCapabilities.WriteNdef | NfcCapabilities.MakeReadOnly | NfcCapabilities.TagIdentifier;


    // android.permission.NFC is a normal permission - granted at install from the manifest, never prompted
    public override Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var adapter = this.Adapter;
        var state = adapter == null
            ? AccessState.NotSupported
            : adapter.IsEnabled
                ? AccessState.Available
                : AccessState.Disabled;

        return Task.FromResult(state);
    }


    protected override Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
    {
        var adapter = this.Adapter ?? throw new NfcNotSupportedException("This device has no NFC hardware", NfcCapabilities.ReadNdef);
        if (!adapter.IsEnabled)
            throw new NfcException("NFC is switched off - ask the user to turn it on in Settings");

        var session = new ReaderModeSession(platform, adapter, sink);
        session.Start();
        return Task.FromResult<IAsyncDisposable>(session);
    }


    sealed class ReaderModeSession(AndroidPlatform platform, NfcAdapter adapter, NfcScanSink sink) : Java.Lang.Object, NfcAdapter.IReaderCallback, IAsyncDisposable
    {
        Activity? activity;
        int disposed;


        public void Start()
        {
            platform.ActivityChanged += this.OnActivityChanged;
            if (platform.CurrentActivity is { } current)
                platform.InvokeOnMainThread(() => this.Enable(current));
        }


        public void OnTagDiscovered(Tag? tag)
        {
            if (tag != null && Volatile.Read(ref this.disposed) == 0)
                sink.Report(new AndroidNfcTag(tag));
        }


        void OnActivityChanged(object? sender, ActivityChanged e)
        {
            if (e.State == ActivityState.Resumed)
                this.Enable(e.Activity);
        }


        void Enable(Activity target)
        {
            if (Volatile.Read(ref this.disposed) != 0 || target.IsFinishing || target.IsDestroyed)
                return;

            try
            {
                adapter.EnableReaderMode(target, this, ReaderFlags, null);
                this.activity = target;
            }
            catch (Exception ex)
            {
                sink.Fail(new NfcException("Android refused to start NFC reader mode", ex));
            }
        }


        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                platform.ActivityChanged -= this.OnActivityChanged;
                platform.InvokeOnMainThread(() =>
                {
                    var target = this.activity;
                    this.activity = null;
                    if (target is { IsDestroyed: false })
                    {
                        try
                        {
                            adapter.DisableReaderMode(target);
                        }
                        catch (Java.Lang.IllegalStateException)
                        {
                            // the activity is no longer resumed - the system already turned reader mode off
                        }
                    }
                });
                sink.Complete();
            }
            return ValueTask.CompletedTask;
        }
    }
}
