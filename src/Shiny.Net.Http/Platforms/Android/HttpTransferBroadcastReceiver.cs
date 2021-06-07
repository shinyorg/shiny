using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    //https://developer.android.com/reference/android/app/DownloadManager
    [BroadcastReceiver(
        Name = "com.shiny.net.http.HttpTransferBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        Native.ActionDownloadComplete
    })]
    public class HttpTransferBroadcastReceiver : ShinyBroadcastReceiver
    {
        public static Subject<HttpTransfer> HttpEvents { get; } = new Subject<HttpTransfer>();


        protected override async Task OnReceiveAsync(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            var delegates = ShinyHost.ResolveAll<IHttpTransferDelegate>();
            if (!delegates.Any())
                return;

            HttpTransfer? transfer = null;
            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            var native = (Native)context.GetSystemService(Context.DownloadService);
            var query = new QueryFilter().Add(id.ToString()).ToNative();

            using (var cursor = native.InvokeQuery(query))
            {
                if (cursor.MoveToNext())
                {
                    transfer = cursor.ToLib();
                    if (transfer.Value.Exception != null)
                    {
                        await delegates.RunDelegates(x => x.OnError(transfer.Value, transfer.Value.Exception));
                    }
                    else
                    {
                        var localUri = cursor.GetString(Native.ColumnLocalUri).Replace("file://", String.Empty);
                        var file = new FileInfo(localUri);

                        await Task.Run(() =>
                        {
                            var to = transfer.Value.LocalFilePath;
                            if (File.Exists(to))
                                File.Delete(to);

                            //File.Copy(localPath, to, true);
                            File.Move(file.FullName, to);
                        });

                        await delegates.RunDelegates(x => x.OnCompleted(transfer.Value));
                    }
                    HttpEvents.OnNext(transfer.Value);
                }
            }
            native.Remove(id);
        }
    }
}
