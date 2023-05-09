// this is derived from dotnet MAUI source code
using System;
using System.Reactive.Linq;
using UIKit;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    public IObservable<IBattery> WhenChanged() => Observable
        .Create<IBattery>(ob =>
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;

            var levelObs = UIDevice.Notifications.ObserveBatteryLevelDidChange((_, _) => ob.OnNext(this));
            var stateObs = UIDevice.Notifications.ObserveBatteryStateDidChange((_, _) => ob.OnNext(this));        
            return () =>
            {
                UIDevice.CurrentDevice.BatteryMonitoringEnabled = false;
                stateObs.Dispose();
                levelObs.Dispose();
            };
        })
        .StartWith(this)
        .Publish()
        .RefCount();

    ////        saverStatusObserver = NSNotificationCenter.DefaultCenter.AddObserver(NSProcessInfo.PowerStateDidChangeNotification, PowerChangedNotification);
    ////    public EnergySaverStatus EnergySaverStatus =>

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
