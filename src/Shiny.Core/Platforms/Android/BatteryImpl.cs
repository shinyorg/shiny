using System;
using System.Threading;
using Android.Content;
using Android.OS;
using Shiny.Power;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    readonly AndroidPlatform platform;
    BroadcastReceiver? receiver;
    int subscriberCount;

    public BatteryImpl(AndroidPlatform platform) => this.platform = platform;


    event EventHandler? changed;
    public event EventHandler? Changed
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
        this.receiver = ActionBroadcastReceiver.Register(
            this.platform,
            Intent.ActionBatteryChanged,
            _ => this.changed?.Invoke(this, EventArgs.Empty)
        );
    }


    void StopListening()
    {
        if (this.receiver != null)
        {
            ActionBroadcastReceiver.UnRegister(this.platform, this.receiver);
            this.receiver = null;
        }
    }


    public BatteryState Status
    {
        get
        {
            var value = this.platform.GetIntentValue(Intent.ActionBatteryChanged, x => x.GetIntExtra(BatteryManager.ExtraStatus, -1));
            return value switch
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
            var values = this.platform.GetIntentValue<(int Level, int Scale)>(Intent.ActionBatteryChanged, intent =>
            (
                intent.GetIntExtra(BatteryManager.ExtraLevel, -1),
                intent.GetIntExtra(BatteryManager.ExtraScale, -1)
            ));

            if (values.Scale <= 0)
                return 1.0;

            return (double)values.Level / (double)values.Scale;
        }
    }
}
