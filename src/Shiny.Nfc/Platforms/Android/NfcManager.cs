using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android;
using Android.App;
using Android.Content;
using Android.Nfc;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Nfc;


//https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
public class NfcManager : Java.Lang.Object, INfcManager, IAndroidLifecycle.IOnActivityNewIntent
{
    readonly Subject<INfcTag> tagSubject = new();
    readonly AndroidPlatform platform;
    readonly ILogger logger;


    public NfcManager(AndroidPlatform platform, ILogger<NfcManager> logger)
    {
        this.platform = platform;
        this.logger = logger;
    }


    public void Handle(Activity activity, Intent intent)
    {
        var isNfc = intent.Action?.Equals(NfcAdapter.ActionTagDiscovered, StringComparison.InvariantCultureIgnoreCase) ?? false;
        if (!isNfc)
            return;

        var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
        if (tag == null)
            return;

        this.logger.LogDebug("Incoming NDEF Discovered action");
        var wrapTag = new DroidNfcTag(tag!);
        this.tagSubject.OnNext(wrapTag);
    }


    public IObservable<AccessState> RequestAccess()
    {
        var ad = NfcAdapter.GetDefaultAdapter(this.platform.AppContext);
        if (ad == null)
            return Observable.Return(AccessState.NotSupported);

        if (!ad.IsEnabled)
            return Observable.Return(AccessState.Disabled);

        return this.platform.RequestAccess(Manifest.Permission.Nfc);
    }


    public IObservable<INfcTag[]> WhenTagsDetected() => Observable.Create<INfcTag[]>(ob =>
    {
        // TODO: hot/cold observable?
        var launchIntent = new Intent(this.platform.CurrentActivity, this.platform.CurrentActivity.GetType());
        //launchIntent.AddFlags(ActivityFlags.SingleTop);

        var pendingIntent = PendingIntent.GetActivity(
            this.platform.AppContext,
            200,
            launchIntent,
            this.platform.GetPendingIntentFlags(0)
        );

        var tagFilter = new IntentFilter(NfcAdapter.ActionTagDiscovered);
        tagFilter.AddCategory(Intent.CategoryDefault);

        //var discoverFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
        //discoverFilter.AddDataType("*/*");

        var adapter = NfcAdapter.GetDefaultAdapter(this.platform.AppContext)!;
        adapter.EnableForegroundDispatch(
            this.platform.CurrentActivity,
            pendingIntent,
            //new [] { tagFilter, discoverFilter },
            new [] { tagFilter },
            null
        );
        var sub = this.tagSubject.Subscribe(x => ob.OnNext(new[] { x }));

        return () =>
        {
            sub?.Dispose();
            adapter.DisableForegroundDispatch(this.platform.CurrentActivity);
        };
    });
}
