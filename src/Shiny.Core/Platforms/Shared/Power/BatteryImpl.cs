#if APPLE || ANDROID
using System;
using System.Reactive.Linq;
using Microsoft.Maui.Devices;
using Battery = Microsoft.Maui.Devices.IBattery;
using OtherState = Microsoft.Maui.Devices.BatteryState;

namespace Shiny.Power;


internal class BatteryImpl : IBattery
{
    readonly Battery battery = Microsoft.Maui.Devices.Battery.Default;

    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        var infoHandler = new EventHandler<BatteryInfoChangedEventArgs>((sender, args) => ob.OnNext(this));
        this.battery.BatteryInfoChanged += infoHandler;

        return () => this.battery.BatteryInfoChanged -= infoHandler;
    });


    public BatteryState Status => this.battery.State switch
    {
        OtherState.Charging => BatteryState.Charging,
        OtherState.Discharging => BatteryState.Discharging,
        OtherState.Full => BatteryState.Full,
        OtherState.NotCharging => BatteryState.NotCharging,
        OtherState.NotPresent => BatteryState.None,
        _ => BatteryState.Unknown
    };


    public double Level => this.battery.ChargeLevel;
}
#endif