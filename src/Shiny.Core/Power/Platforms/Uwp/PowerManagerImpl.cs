using System;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.System.Power;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public bool IsEnergySavingEnabled => PowerManager.EnergySaverStatus == EnergySaverStatus.On;

        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                PowerManager.EnergySaverStatusChanged += this.OnEnergySaverStatusChanged;
                Battery.AggregateBattery.ReportUpdated += this.OnReportUpdated;
            }
            else
            {
                PowerManager.EnergySaverStatusChanged -= this.OnEnergySaverStatusChanged;
                Battery.AggregateBattery.ReportUpdated -= this.OnReportUpdated;
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


        void OnEnergySaverStatusChanged(object sender, object e)
            => this.RaisePropertyChanged(nameof(IsEnergySavingEnabled));

        void OnReportUpdated(Battery sender, object args)
        {
            this.RaisePropertyChanged(nameof(this.BatteryLevel));
            this.RaisePropertyChanged(nameof(this.Status));
        }
    }
}
