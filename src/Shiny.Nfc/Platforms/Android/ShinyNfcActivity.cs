using System;
using Android.App;
using Android.Content;
using Android.Nfc;


namespace Shiny.Nfc
{
    [IntentFilter(
        new[] { NfcAdapter.ActionNdefDiscovered },
        //Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "application/com.shiny.nfc"
    )]
    public class ShinyNfcActivity : Activity
    {
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            NfcManager.OnNewIntent(intent);
            this.Finish();
        }
    }
}
