using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android;
using Android.App;
using Android.Content;
using Android.Nfc;
using Microsoft.Extensions.Logging;


namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : Java.Lang.Object, INfcManager
    {
        readonly IPlatform platform;
        readonly Subject<INfcTag> tagSubject;


        public NfcManager(IPlatform platform, ILogger<NfcManager> logger)
        {
            this.tagSubject = new Subject<INfcTag>();
            this.platform = platform;

            this.platform
                .WhenIntentReceived()
                .Where(x =>
                    x.NewIntent &&
                    //(x.Action?.Equals(NfcAdapter.ActionNdefDiscovered, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (x.Intent.Action?.Equals(NfcAdapter.ActionTagDiscovered, StringComparison.InvariantCultureIgnoreCase) ?? false)
                )
                .Select(x => x.Intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag)
                .Where(tag => tag != null)
                .Subscribe(tag =>
                {
                    logger.LogDebug("Incoming NDEF Discovered action");
                    var wrapTag = new DroidNfcTag(tag!);
                    this.tagSubject.OnNext(wrapTag);
                });
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
}
