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
        public static IObservable<object> WhenReceived() => receiveSubject;
        static Subject<object> receiveSubject = new Subject<object>();


        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Native.ActionDownloadComplete)
                return;

            var tdelegate = ShinyHost.Resolve<IHttpTransferDelegate>();
            var id = intent.GetLongExtra(Native.ExtraDownloadId, -1);
            //ShinyHost.Resolve<IRepository>()
        }
    }
}
