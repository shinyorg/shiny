using System;
using Android.App;
using Android.Content;
using Android.OS;

[assembly: UsesPermission(Android.Manifest.Permission.BatteryStats)]

namespace Shiny.Jobs.Power;


public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
{
    readonly AndroidPlatform platform;
    IDisposable? dispose;


    public PowerManagerImpl(AndroidPlatform platform) => this.platform = platform;


    protected override void OnNpcHookChanged(bool hasSubscribers)
    {
        if (hasSubscribers)
        {
            this.dispose = this.platform
                .WhenIntentReceived(Intent.ActionBatteryChanged)
                .Subscribe(_ =>
                {
                    this.RaisePropertyChanged(nameof(this.Status));
                    this.RaisePropertyChanged(nameof(this.BatteryLevel));
                });
        }
        else
        {
            this.dispose?.Dispose();
        }
    }


    public bool IsEnergySavingEnabled => this.platform.GetSystemServiceValue<bool, PowerManager>(
        Context.PowerService,
        pm =>
            pm.IsDeviceIdleMode &&
            !pm.IsIgnoringBatteryOptimizations(this.platform.Package.PackageName)
    );
    public PowerState Status => this.platform.GetIntentValue(Intent.ActionBatteryChanged, ToState);
    public int BatteryLevel => this.platform.GetIntentValue(Intent.ActionBatteryChanged, ToLevel);


    static PowerState ToState(Intent intent)
    {
        var status = (BatteryStatus)intent.GetIntExtra(BatteryManager.ExtraStatus, -1);
        return status switch
        {
            BatteryStatus.Discharging => PowerState.Discharging,
            BatteryStatus.NotCharging => PowerState.Discharging,
            BatteryStatus.Charging => PowerState.Charging,
            BatteryStatus.Full => PowerState.Charged,
            _ => PowerState.Unknown
        };
    }


    static int ToLevel(Intent intent)
    {
        var level = intent.GetIntExtra(BatteryManager.ExtraLevel, -1);
        var scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);

        return (int) Math.Floor(level * 100D / scale);
    }
}