using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.Logging;
using Shiny.Net.Http.Infrastructure;

namespace Shiny.Net.Http;


[Android.App.Service(
    Enabled = true,
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeDataSync
)]
public class HttpTransferService : ShinyAndroidForegroundService<IHttpTransferManager, IHttpTransferDelegate>
{
    public static bool IsStarted { get; private set; }

    /// <summary>
    /// The running instance, or null when the service is stopped. Exposed so
    /// <see cref="ForegroundNotificationRenderer"/> can draw transfer progress onto this service's own
    /// notification instead of posting a second one.
    /// </summary>
    public static HttpTransferService? Current { get; private set; }

    /// <summary>
    /// The id of the notification this service was promoted to the foreground with. Re-posting this id is
    /// how the notification gets updated in place.
    /// </summary>
    public int ForegroundNotificationId => this.NotificationId;

    /// <summary>
    /// When true (and the device runs Android 14+), the service promotes itself as
    /// FOREGROUND_SERVICE_TYPE_SHORT_SERVICE instead of dataSync. shortService needs
    /// no type-specific manifest permission and no Google Play foreground-service
    /// declaration, at the cost of a ~3 minute run window per promotion (extended
    /// while the app is visible). Suits apps whose transfers are brief bursts —
    /// pending transfers survive a timeout in the repository and the service is
    /// re-armed on the next Queue()/app start. Apps opting in can also strip
    /// android:foregroundServiceType + FOREGROUND_SERVICE_DATA_SYNC from their
    /// merged manifest (shortService does not need to be declared).
    /// On Android 13 and lower this flag is ignored (the type does not exist there;
    /// pre-14 devices keep dataSync).
    /// </summary>
    public static bool UseShortService { get; set; }

    protected override ForegroundService StartForegroundServiceType =>
        UseShortService && System.OperatingSystem.IsAndroidVersionAtLeast(34)
            ? ForegroundService.TypeShortService
            : ForegroundService.TypeDataSync;


    protected override void OnStart(Intent? intent)
    {
        Current = this;
        if (!IsStarted)
        {
            this.GetService<HttpClientHttpTransferProcess>().Run(() => this.Stop());
            IsStarted = true;
        }
    }


    protected override void OnStop()
    {
        IsStarted = false;
        Current = null;
    }

    /// <summary>
    /// The OS says this service's time is up: shortService gets ~3 minutes per
    /// promotion (API 34+), and Android 15 caps dataSync at a 6-hour daily budget —
    /// both deliver onTimeout, and NOT stopping promptly is an ANR
    /// ("A foreground service of ... did not stop within its timeout").
    /// Pending transfers stay in the repository; the manager re-arms the service on
    /// the next Queue() or app start, so a timeout defers work instead of losing it.
    /// </summary>
    public override void OnTimeout(int startId)
    {
        this.Logger.LogWarning(
            "HttpTransferService hit its foreground-service time limit — stopping; pending transfers resume on the next service start");
        this.Stop();
    }

    /// <inheritdoc cref="OnTimeout(int)"/>
    public override void OnTimeout(int startId, ForegroundService fgsType)
        => this.OnTimeout(startId);

    public override IBinder? OnBind(Intent? intent) => null;
}
