using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Shiny.Data.Sync.Infrastructure;

namespace Shiny.Data.Sync;


[Android.App.Service(
    Enabled = true,
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeDataSync
)]
public class DataSyncService : ShinyAndroidForegroundService<IDataSyncManager, IDataSyncDelegate>
{
    public static bool IsStarted { get; private set; }
    protected override ForegroundService StartForegroundServiceType => ForegroundService.TypeDataSync;


    protected override void OnStart(Intent? intent)
    {
        if (!IsStarted)
        {
            this.GetService<HttpClientDataSyncProcess>().Run(() => this.Stop());
            IsStarted = true;
        }
    }


    protected override void OnStop() => IsStarted = false;

    public override IBinder? OnBind(Intent? intent) => null;
}
