using System;
using System.IO;
using Android.App;
using Android.Content;
using Shiny.Logging;
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
            var native = context.GetManager();

            try
            {
                var tdelegate = ShinyHost.Resolve<IHttpTransferDelegate>();

                var query = new QueryFilter().Add(id.ToString()).ToNative();
                using (var cursor = native.InvokeQuery(query))
                {
                    if (cursor.MoveToNext())
                    {
                        var transfer = cursor.ToLib();
                        var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));
                        File.Move(localPath, transfer.LocalFilePath);

                        tdelegate.OnCompleted(transfer);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex, ("TransferId", id.ToString()));
            }

            native.Remove(id);
        }
    }
}
