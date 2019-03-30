using System;
using System.Reactive.Subjects;
using Android.App;
using Android.Content;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    //https://developer.android.com/reference/android/app/DownloadManager
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] {
        Native.ActionDownloadComplete,
        Native.ActionNotificationClicked,
        Native.ActionViewDownloads
    })]
    public class HttpTransferBroadcastReceiver : BroadcastReceiver
    {
        public static IObservable<object> WhenReceived() => receiveSubject;
        static Subject<object> receiveSubject = new Subject<object>();


        public override void OnReceive(Context context, Intent intent)
        {
            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            switch (intent.Action)
            {
                case Native.ActionDownloadComplete:
                    break;

                case Native.ActionNotificationClicked:
                    break;

                case Native.ActionViewDownloads:
                    break;

                    //long id = intent.getLongExtra(DownloadManager.EXTRA_DOWNLOAD_ID, -1);
                    //DownloadManager.ColumnLocalFilename
                    //DownloadManager.ColumnBytesDownloadedSoFar
                    //DownloadManager.ColumnTotalSizeBytes
                    //DownloadManager.ColumnLastModifiedTimestamp
            }
            //File file = new File(getExternalFilesDir(null), "Dummy");
        }
    }
}
