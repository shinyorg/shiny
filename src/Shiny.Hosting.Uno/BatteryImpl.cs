using System;
using System.Reactive.Linq;
using Windows.Devices.Power;
using Windows.System.Power;

namespace Shiny;


public class BatteryImpl : Shiny.Power.IBattery
{
    public BatteryState Status
    {
        get
        {
            var report = Battery.AggregateBattery.GetReport();

            switch (report.Status)
            {
                case BatteryStatus.Charging:
                    return BatteryState.Charging;
                case BatteryStatus.Discharging:
                    return BatteryState.Discharging;
                case BatteryStatus.Idle:
                    if (this.Level >= 1.0)
                        return BatteryState.Full;
                    return BatteryState.NotCharging;

                case BatteryStatus.NotPresent:
                    return BatteryState.None;
            }

            if (this.Level >= 1.0)
                return BatteryState.Full;

            return BatteryState.Unknown;
        }
    }


    public double Level
    {
        get
        {
            var finalReport = Battery.AggregateBattery.GetReport();
            var finalPercent = 1.0;

            var remaining = finalReport.RemainingCapacityInMilliwattHours;
            var full = finalReport.FullChargeCapacityInMilliwattHours;

            if (remaining.HasValue && full.HasValue)
                finalPercent = (double)remaining.Value / (double)full.Value;

            return finalPercent;
        }
    }


    public IObservable<Shiny.Power.IBattery> WhenChanged() => Observable.Create<Shiny.Power.IBattery>(ob =>
    {
        var handler = new Windows.Foundation.TypedEventHandler<Battery, object>((_, _) => ob.OnNext(this));
        Battery.AggregateBattery.ReportUpdated += handler;

        return () => Battery.AggregateBattery.ReportUpdated -= handler;
    });
}