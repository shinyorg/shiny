using System;
using Shiny.Power;
using Xamarin.Essentials;


namespace Shiny.Integrations.XamEssentials
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public PowerState Status => Battery.State switch
        {
            BatteryState.Discharging => PowerState.Discharging,
            BatteryState.Charging => PowerState.Charging,
            BatteryState.Full => PowerState.Charged,
            BatteryState.NotPresent => PowerState.NoBattery,
            BatteryState.NotCharging => PowerState.Discharging,
            BatteryState.Unknown => PowerState.Unknown
        };
        public int BatteryLevel => Convert.ToInt32(Battery.ChargeLevel * 100);
        public bool IsEnergySavingEnabled => Battery.EnergySaverStatus == EnergySaverStatus.On;


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                Battery.BatteryInfoChanged += this.OnBatteryChanged;
            }
            else
            {
                Battery.BatteryInfoChanged -= this.OnBatteryChanged;
            }
        }


        void OnBatteryChanged(object sender, BatteryInfoChangedEventArgs e)
            => this.RaisePropertyChanged();
    }
}
