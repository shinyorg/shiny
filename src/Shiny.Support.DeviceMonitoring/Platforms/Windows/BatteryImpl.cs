using System;
using System.Reactive.Linq;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.System.Power;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    public BatteryState Status => Battery.AggregateBattery.GetReport().Status switch
    {
        BatteryStatus.Charging => BatteryState.Charging,
        BatteryStatus.Discharging => BatteryState.Discharging,
        BatteryStatus.Idle => BatteryState.Full,
        BatteryStatus.NotPresent => BatteryState.None,
        _ => BatteryState.Unknown
    };


    public double Level
    {
        get
        {
            var report = Battery.AggregateBattery.GetReport();
            var remain = report.RemainingCapacityInMilliwattHours;
            var full = report.FullChargeCapacityInMilliwattHours;

            if (remain == null || full == null)
                return -1;

            var value = (int)(remain.Value / (double)full.Value * 100);
            return value;
        }
    }


    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        var handler = new TypedEventHandler<Battery, object>((_, _) => ob.OnNext(this));
        Battery.AggregateBattery.ReportUpdated += handler;

        return () => Battery.AggregateBattery.ReportUpdated -= handler;
    });
}