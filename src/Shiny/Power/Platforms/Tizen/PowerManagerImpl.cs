using System;
using Tizen.System;

namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public bool IsEnergySavingEnabled => false;

        public PowerState Status
        {
            get
            {
                if (Battery.Percent == 100)
                    return Battery.IsCharging
                        ? PowerState.Charged
                        : PowerState.Discharging;

                return Battery.IsCharging
                    ? PowerState.Charging
                    : PowerState.Discharging;
            }
        }


        public int BatteryLevel => Battery.Percent;


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                Battery.PercentChanged += this.OnPercentChanged;
                Battery.ChargingStateChanged += this.OnChargingStateChanged;
                Battery.LevelChanged += this.OnLevelChanged;
            }
            else
            {
                Battery.PercentChanged -= this.OnPercentChanged;
                Battery.ChargingStateChanged -= this.OnChargingStateChanged;
                Battery.LevelChanged -= this.OnLevelChanged;
            }
        }


        void OnLevelChanged(object sender, BatteryLevelChangedEventArgs e) => this.OnChanged();
        void OnChargingStateChanged(object sender, BatteryChargingStateChangedEventArgs e) => this.OnChanged();
        void OnPercentChanged(object sender, BatteryPercentChangedEventArgs e) => this.OnChanged();
        void OnChanged()
        {
            this.RaisePropertyChanged(nameof(this.BatteryLevel));
            this.RaisePropertyChanged(nameof(this.Status));
        }
    }
}