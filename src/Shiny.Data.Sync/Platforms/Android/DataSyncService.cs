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
            IsStarted = true;
            this.RunLoop();
        }
    }


    void RunLoop() => this.GetService<HttpClientDataSyncProcess>().Run(() =>
    {
        // the loop only exits once the outbox is empty - an op queued while it was shutting down saw
        // IsStarted == true and didn't start the service, so run the loop again instead of stopping
        var queued = this.GetService<Shiny.Extensions.Stores.Repositories.IRepository>().GetAll<SyncOperation>().Count;
        if (IsStarted && queued > 0)
            this.RunLoop();
        else
            this.Stop();
    });


    protected override void OnStop() => IsStarted = false;

    public override IBinder? OnBind(Intent? intent) => null;
}
