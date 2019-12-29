using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Shiny.Logging;

namespace Shiny.Nfc
{
    //https://developer.android.com/guide/topics/connectivity/nfc/nfc.html
    public class NfcManager : INfcManager
    {
        readonly NfcAdapter adapter;
        readonly AndroidContext context;

        

        public NfcManager(AndroidContext context)
        {
            this.context = context;
            this.adapter = NfcAdapter.GetDefaultAdapter(context.AppContext);
        }


        public static async void OnNewIntent(Intent intent)
        {
            if (intent == null)
                return;

            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag != null)
            {
                var message = Ndef.Get(tag).CachedNdefMessage;
                var records = message
                    .GetRecords()
                    .Select(x => new ShinyNDefRecord(x))
                    .ToArray();

                await Log.SafeExecute(() => ShinyHost.Resolve<INfcDelegate>().OnReceived(records));
            }
        }


        public bool IsListening => this.adapter.IsEnabled;


        public void StartListener()
        {
            var activity = this.context.CurrentActivity;

            var intent = new Intent(activity, activity.GetType());
            var pendingIntent = PendingIntent.GetActivity(activity, 0, intent, 0);

            var ndef = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
            ndef.AddDataType("*/*");

            var tag = new IntentFilter(NfcAdapter.ActionTagDiscovered);
            tag.AddCategory(Intent.CategoryDefault);

            this.adapter.EnableForegroundDispatch(
                activity,
                pendingIntent,
                new IntentFilter[] { ndef, tag },
                null
            );
        }


        public void StopListening()
            => this.adapter.DisableForegroundDispatch(this.context.CurrentActivity);


        public Task<AccessState> RequestAccess(bool forWrite = false) => this.context
            .RequestAccess(Manifest.Permission.Nfc)
            .ToTask();
    }
}
