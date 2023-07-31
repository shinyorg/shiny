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
    int notificationId = 9999;

    protected override void OnStart(Intent? intent)
    {
        this.notificationId++;
        this.GetService<HttpTransferProcess>().Run(new(
            this.notificationId,
            this.Builder!,
            this.NotificationManager!,
            () => this.Stop()
        ));
        IsStarted = true;
    }


    protected override void OnStop() => IsStarted = false;

    public override IBinder? OnBind(Intent? intent) => null;
}