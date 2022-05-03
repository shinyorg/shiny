using System;
using System.Reactive.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Nfc;


namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : Java.Lang.Object, INfcManager
    {
        readonly IPlatform platform;


        public NfcManager(IPlatform platform)
        {
            this.platform = platform;
            this.platform
                .WhenIntentReceived()
                // TODO: others
                .Where(x => x.Action?.Equals(NfcAdapter.ActionNdefDiscovered, StringComparison.InvariantCultureIgnoreCase) ?? false)
                .Subscribe(x =>
                {
                    // TODO: we should hang on to any tags discovered here if we can't push it out to a subject
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
            var launchIntent = new Intent(this.platform.CurrentActivity, this.platform.CurrentActivity.GetType());
            launchIntent.AddFlags(ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                this.platform.AppContext,
                200,
                launchIntent,
                this.platform.GetPendingIntentFlags(0)
            );

            var tagFilter = new IntentFilter(NfcAdapter.ActionTagDiscovered);
            tagFilter.AddCategory(Intent.CategoryDefault);

            var discoverFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
            discoverFilter.AddDataType("*/*");

            var adapter = NfcAdapter.GetDefaultAdapter(this.platform.AppContext)!;
            adapter.EnableForegroundDispatch(
                this.platform.CurrentActivity,
                pendingIntent,
                new [] { tagFilter, discoverFilter },
                null
            );
            return () => adapter.DisableForegroundDispatch(this.platform.CurrentActivity);
        });
    }
}
