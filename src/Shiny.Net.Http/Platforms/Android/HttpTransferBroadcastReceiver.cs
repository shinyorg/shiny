using System;
using System.Reactive.Subjects;
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
        public static IObservable<IHttpTransfer> WhenCompleted() => compSubj;
        static Subject<IHttpTransfer> compSubj = new Subject<IHttpTransfer>();


        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            //compSubj.OnNext(null);
            //ShinyHost
            //    .Resolve<IHttpTransferDelegate>()
            //    .OnCompleted(null);
        }
    }
}
