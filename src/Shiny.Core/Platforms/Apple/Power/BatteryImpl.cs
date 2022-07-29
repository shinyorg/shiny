using System;
using System.Reactive.Linq;
using UIKit;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    public BatteryState Status
    {
        get
        {
            var e = UIDevice.CurrentDevice.BatteryMonitoringEnabled;
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;

            try
            {
                var state = UIDevice.CurrentDevice.BatteryState switch
                {
                    UIDeviceBatteryState.Charging => BatteryState.Charging,
                    UIDeviceBatteryState.Full => BatteryState.Full,
                    UIDeviceBatteryState.Unplugged => BatteryState.Discharging,
                    _ => UIDevice.CurrentDevice.BatteryLevel >= 1.0 ? BatteryState.Full : BatteryState.Unknown
                };
                return state;
            }
		    finally
            {
                UIDevice.CurrentDevice.BatteryMonitoringEnabled = e;
            }
        }
    }


    public double Level
    {
        get
        {
            var e = UIDevice.CurrentDevice.BatteryMonitoringEnabled;
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;

            try
            {
                return UIDevice.CurrentDevice.BatteryLevel;
            }
            finally
            {
                UIDevice.CurrentDevice.BatteryMonitoringEnabled = e;
            }
        }
    }


    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
        var levelObserver = UIDevice.Notifications.ObserveBatteryLevelDidChange((__, _) => ob.OnNext(this));
        var stateObserver = UIDevice.Notifications.ObserveBatteryStateDidChange((__, _) => ob.OnNext(this));

        return () =>
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = false;
            levelObserver.Dispose();
            stateObserver.Dispose();
        };
    });
}
