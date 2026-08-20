using Android.Content;
using Android.Provider;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Wifi;


/// <summary>
/// Android airplane mode - readable, not settable.
/// </summary>
/// <remarks>
/// <c>Settings.Global.AIRPLANE_MODE_ON</c> has been readable and write-protected since Android 4.2;
/// writing it needs <c>WRITE_SECURE_SETTINGS</c>, which is signature-level and cannot be granted to
/// an installed app. <see cref="OpenSettings"/> opens the airplane mode settings screen, which is
/// the supported way to let a user change it.
/// </remarks>
public class AndroidAirplaneMode(
    AndroidPlatform platform,
    ILogger<AndroidAirplaneMode> logger
) : IAirplaneMode
{
    AirplaneModeReceiver? receiver;
    int subscriberCount;

    public bool IsSupported => true;
    public bool CanToggle => false;


    public bool IsEnabled
        => Settings.Global.GetInt(platform.AppContext.ContentResolver, Settings.Global.AirplaneModeOn, 0) != 0;


    event EventHandler<bool>? changed;
    public event EventHandler<bool>? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
                this.StartListening();
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
                this.StopListening();
        }
    }


    void StartListening()
    {
        this.receiver = new AirplaneModeReceiver(() => this.changed?.Invoke(this, this.IsEnabled));
        var filter = new IntentFilter(Intent.ActionAirplaneModeChanged);
        platform.AppContext.RegisterReceiver(this.receiver, filter);
        logger.WatcherStarted(Intent.ActionAirplaneModeChanged);
    }


    void StopListening()
    {
        if (this.receiver != null)
        {
            platform.AppContext.UnregisterReceiver(this.receiver);
            this.receiver = null;
        }
    }


    public Task SetEnabled(bool enabled, CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.AirplaneModeToggle,
            "Android 4.2 made AIRPLANE_MODE_ON write-protected behind WRITE_SECURE_SETTINGS, a signature permission. Send the user to Settings with OpenSettings() instead"
        );


    public Task OpenSettings(CancellationToken ct = default)
    {
        var intent = new Intent(Settings.ActionAirplaneModeSettings);

        // started from the application context rather than an activity, so it needs its own task
        intent.SetFlags(ActivityFlags.NewTask);
        platform.AppContext.StartActivity(intent);
        return Task.CompletedTask;
    }
}


[BroadcastReceiver(Enabled = true, Exported = false)]
class AirplaneModeReceiver(Action onChanged) : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) => onChanged();
}
