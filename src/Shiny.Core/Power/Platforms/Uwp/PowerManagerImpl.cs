using System;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.System.Power;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        IDisposable dispose;


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                this.dispose = this.WhenChanged().Subscribe(_ =>
                {
                    this.OnPropertyChanged(nameof(this.BatteryLevel));
                    this.OnPropertyChanged(nameof(this.Status));
                });
            }
            else
            {
                this.dispose?.Dispose();
            }
        }


        public int BatteryLevel
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


        public PowerState Status
        {
            get
            {
                var report = Battery.AggregateBattery.GetReport();
                switch (report.Status)
                {
                    case BatteryStatus.Charging:
                        return PowerState.Charging;

                    case BatteryStatus.Discharging:
                        return PowerState.Discharging;

                    case BatteryStatus.Idle:
                        return PowerState.Charged;

                    case BatteryStatus.NotPresent:
                        return PowerState.NoBattery;

                    default:
                        return PowerState.Unknown;
                }
            }
        }


        IObservable<Unit> WhenChanged() => Observable.Create<Unit>(ob =>
        {
            var handler = new TypedEventHandler<Battery, object>((sender, args) => ob.OnNext(Unit.Default));
            Battery.AggregateBattery.ReportUpdated += handler;
            return () => Battery.AggregateBattery.ReportUpdated -= handler;
        });
    }
}
