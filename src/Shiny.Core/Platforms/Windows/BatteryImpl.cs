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
    EventHandler<object>? powerHandler;
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

                this.powerHandler = (_, _) => this.changed?.Invoke(this, EventArgs.Empty);
                PowerManager.PowerSupplyStatusChanged += this.powerHandler;
                PowerManager.EnergySaverStatusChanged += this.powerHandler;
            }
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0 && this.handler != null)
            {
                Battery.AggregateBattery.ReportUpdated -= this.handler;
                this.handler = null;

                PowerManager.PowerSupplyStatusChanged -= this.powerHandler;
                PowerManager.EnergySaverStatusChanged -= this.powerHandler;
                this.powerHandler = null;
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


    // Windows does not say whether mains power arrives over AC or USB-C
    public BatteryPowerSource PowerSource => PowerManager.PowerSupplyStatus switch
    {
        PowerSupplyStatus.NotPresent => BatteryPowerSource.Battery,
        PowerSupplyStatus.Adequate or PowerSupplyStatus.Inadequate => BatteryPowerSource.AC,
        _ => BatteryPowerSource.Unknown
    };


    public EnergySaverStatus EnergySaverStatus => PowerManager.EnergySaverStatus switch
    {
        global::Windows.System.Power.EnergySaverStatus.On => EnergySaverStatus.On,
        global::Windows.System.Power.EnergySaverStatus.Off or global::Windows.System.Power.EnergySaverStatus.Disabled => EnergySaverStatus.Off,
        _ => EnergySaverStatus.Unknown
    };
}
