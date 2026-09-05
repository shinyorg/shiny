// derived from dotnet MAUI source code
#if TVOS
using System;
using Shiny.Power;

namespace Shiny.Power;


/// <summary>
/// Apple TV runs on mains power and UIDevice carries none of the battery API on tvOS, so this
/// reports a permanently full battery rather than pretending to monitor one.
/// </summary>
public class BatteryImpl : IBattery
{
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public BatteryState Status => BatteryState.Full;
    public double Level => 1.0;
}
#else
using System;
using System.Threading;
using UIKit;
using Shiny.Power;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    IDisposable? levelObs;
    IDisposable? stateObs;
    int subscriberCount;


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
        UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
        this.levelObs = UIDevice.Notifications.ObserveBatteryLevelDidChange((_, _) => this.changed?.Invoke(this, EventArgs.Empty));
        this.stateObs = UIDevice.Notifications.ObserveBatteryStateDidChange((_, _) => this.changed?.Invoke(this, EventArgs.Empty));
    }


    void StopListening()
    {
        UIDevice.CurrentDevice.BatteryMonitoringEnabled = false;
        this.stateObs?.Dispose();
        this.stateObs = null;
        this.levelObs?.Dispose();
        this.levelObs = null;
    }


    public BatteryState Status
    {
        get
        {
            var dev = UIDevice.CurrentDevice;
            var origState = dev.BatteryMonitoringEnabled;
            dev.BatteryMonitoringEnabled = true;
            var result = dev.BatteryState switch
            {
                UIDeviceBatteryState.Charging => BatteryState.Charging,
                UIDeviceBatteryState.Full => BatteryState.Full,
                UIDeviceBatteryState.Unplugged => BatteryState.Discharging,
                _ => dev.BatteryLevel >= 1.0 ? BatteryState.Full : BatteryState.Unknown
            };
            dev.BatteryMonitoringEnabled = origState;
            return result;
        }
    }


    public double Level
    {
        get
        {
            var dev = UIDevice.CurrentDevice;
            var origState = dev.BatteryMonitoringEnabled;
            dev.BatteryMonitoringEnabled = true;
            var result = dev.BatteryLevel;
            dev.BatteryMonitoringEnabled = origState;
            return result;
        }
    }
}
#endif
