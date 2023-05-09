#if XAMARINIOS || MONOANDROID
using System;
using System.Reactive.Linq;
using Xamarin.Essentials;
using Battery = Xamarin.Essentials.Battery;
using OtherState = Xamarin.Essentials.BatteryState;

namespace Shiny.Power;


internal class BatteryImpl : IBattery
{
    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        var infoHandler = new EventHandler<BatteryInfoChangedEventArgs>((sender, args) => ob.OnNext(this));
        Battery.BatteryInfoChanged += infoHandler;

        return () => Battery.BatteryInfoChanged -= infoHandler;
    });


    public BatteryState Status => Battery.State switch
    {
        OtherState.Charging => BatteryState.Charging,
        OtherState.Discharging => BatteryState.Discharging,
        OtherState.Full => BatteryState.Full,
        OtherState.NotCharging => BatteryState.NotCharging,
        OtherState.NotPresent => BatteryState.None,
        _ => BatteryState.Unknown
    };


    public double Level => Battery.ChargeLevel;
}
#endif