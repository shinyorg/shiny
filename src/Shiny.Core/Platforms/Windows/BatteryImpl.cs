using System;
using System.Threading;
using Shiny.Power;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.System.Power;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    TypedEventHandler<Battery, object>? handler;
    int subscriberCount;


    event EventHandler? changed;
    public event EventHandler? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
            {
                this.handler = (_, _) => this.changed?.Invoke(this, EventArgs.Empty);
                Battery.AggregateBattery.ReportUpdated += this.handler;
            }
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0 && this.handler != null)
            {
                Battery.AggregateBattery.ReportUpdated -= this.handler;
                this.handler = null;
            }
        }
    }


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

            return remain.Value / (double)full.Value;
        }
    }
}
