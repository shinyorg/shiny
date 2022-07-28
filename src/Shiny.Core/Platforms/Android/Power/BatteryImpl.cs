using System;
using System.Reactive.Linq;
using Android.Content;
using Android.OS;

namespace Shiny.Power;


public class BatteryImpl : IBattery, IShinyStartupTask
{
    readonly AndroidPlatform platform;
    public BatteryImpl(AndroidPlatform platform) => this.platform = platform;


    public void Start()
    {
        if (!this.platform.IsInManifest(Android.Manifest.Permission.BatteryStats))
            throw new InvalidOperationException("android.manifest.BATTERY_STATS is missing from your android manifest");
    }


    public BatteryState Status
    {
        get
        {
            var state = this.platform.GetIntentValue(
                Intent.ActionBatteryChanged,
                x => x.GetIntExtra(BatteryManager.ExtraStatus, -1)
            );
            return state switch
            {
                (int)BatteryStatus.Charging => BatteryState.Charging,
                (int)BatteryStatus.Discharging => BatteryState.Discharging,
                (int)BatteryStatus.Full => BatteryState.Full,
                (int)BatteryStatus.NotCharging => BatteryState.NotCharging,
                _ => BatteryState.Unknown
            };
        }
    }


    public double Level
    {
        get
        {
            var scale = 0;
            var level = this.platform.GetIntentValue(Intent.ActionBatteryChanged, intent =>
            {
                scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);
                return intent.GetIntExtra(BatteryManager.ExtraLevel, -1);
            });
            if (scale <= 0)
                return 1.0;

            return (double)level / (double)scale;
        }
    }


    public IObservable<IBattery> WhenChanged() => this.platform
        .WhenIntentReceived(Intent.ActionBatteryChanged)
        .Select(_ => this);
}