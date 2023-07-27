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
public class HttpTransferService : ShinyAndroidForegroundService
{
    public static bool IsStarted { get; private set; }


    protected override void OnStart(Intent? intent)
    {
        IsStarted = true;
    }


    protected override void OnStop() => IsStarted = false;
    public override IBinder? OnBind(Intent? intent) => null;
}