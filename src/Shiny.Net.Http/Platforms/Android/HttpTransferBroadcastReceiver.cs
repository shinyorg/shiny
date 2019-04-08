using System;
using Android.App;
using Android.Content;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    //https://developer.android.com/reference/android/app/DownloadManager
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Native.ActionDownloadComplete })]
    public class HttpTransferBroadcastReceiver : BroadcastReceiver
    {
        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            var manager = ShinyHost.Resolve<IHttpTransferManager>();
            var transfer = await manager.GetTransfer(id.ToString());

            ShinyHost
                .Resolve<IHttpTransferDelegate>()
                .OnCompleted(transfer);

            context.GetManager().Remove(id);
        }
    }
}
