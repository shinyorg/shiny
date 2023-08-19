using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Shiny.Net.Http;


[Service(
    Enabled = true,
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeDataSync
)]
public class HttpTransferService : ShinyAndroidForegroundService<IHttpTransferManager, IHttpTransferDelegate>
{
    public static bool IsStarted { get; private set; }


    protected override void OnStart(Intent? intent)
    {
        this.GetService<HttpTransferProcess>().Run(() => this.Stop());
        IsStarted = true;
    }


    protected override void OnStop() => IsStarted = false;

    public override IBinder? OnBind(Intent? intent) => null;
}