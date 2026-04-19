using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Shiny.Locations;


[Service(
    Enabled = true,
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeLocation
)]
public class ShinyGpsService : ShinyAndroidForegroundService<IGpsManager, IGpsDelegate>
{
    static volatile bool isStarted;
    public static bool IsStarted => isStarted;
    protected override ForegroundService StartForegroundServiceType => ForegroundService.TypeLocation;


    protected override void OnStart(Intent? intent)
    {
        this.Service!
            .WhenReading()
            .SubscribeAsync(
                reading => this.Delegates!.RunDelegates(
                    x => x.OnReading(reading),
                    this.Logger
                )
            )
            .DisposedBy(this.DestroyWith!);

        isStarted = true;
    }


    protected override void OnStop() => isStarted = false;
    public override IBinder? OnBind(Intent? intent) => null;
}
