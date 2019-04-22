using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
        public static Subject<HttpTransfer> HttpEvents { get; } = new Subject<HttpTransfer>();


        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            var native = context.GetManager();
            var tdelegate = ShinyHost.Resolve<IHttpTransferDelegate>();
            HttpTransfer? transfer = null;

            try
            {
                var query = new QueryFilter().Add(id.ToString()).ToNative();
                using (var cursor = native.InvokeQuery(query))
                {
                    if (cursor.MoveToNext())
                    {
                        transfer = cursor.ToLib();
                        var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));
                        await Task.Run(() =>
                        {
                            var to = transfer.Value.LocalFilePath;
                            if (File.Exists(to))
                                File.Delete(to);

                            Console.WriteLine($"Transfer Complete: {localPath} => {to}");
                            //File.Copy(localPath, to, true);
                            File.Move(localPath, to);
                        });

                        tdelegate.OnCompleted(transfer.Value);
                        HttpEvents.OnNext(transfer.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                if (transfer == null)
                    Log.Write(ex);
                else
                {
                    HttpEvents.OnNext(transfer.Value);
                    tdelegate.OnError(transfer.Value, ex);
                }
            }

            native.Remove(id);
        }
    }
}
