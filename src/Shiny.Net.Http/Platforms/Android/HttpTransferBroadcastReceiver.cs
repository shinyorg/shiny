﻿using System;
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
    [BroadcastReceiver(
        Name = "com.shiny.net.http.HttpTransferBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] { Native.ActionDownloadComplete })]
    public class HttpTransferBroadcastReceiver : BroadcastReceiver
    {
        public static Subject<HttpTransfer> HttpEvents { get; } = new Subject<HttpTransfer>();


        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            HttpTransfer? transfer = null;
            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            var native = context.GetManager();
            var tdelegate = ShinyHost.Resolve<IHttpTransferDelegate>();

            try
            {
                var query = new QueryFilter().Add(id.ToString()).ToNative();

                using (var cursor = native.InvokeQuery(query))
                {
                    if (cursor.MoveToNext())
                    {
                        transfer = cursor.ToLib();
                        if (transfer.Value.Exception != null)
                            await tdelegate.OnError(transfer.Value, transfer.Value.Exception);

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

                            await tdelegate.OnCompleted(transfer.Value);
                        }
                        HttpEvents.OnNext(transfer.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);

                if (transfer != null)
                {
                    await tdelegate.OnError(transfer.Value, ex);
                    HttpEvents.OnNext(transfer.Value);
                }
            }
            native.Remove(id);
        }
    }
}
