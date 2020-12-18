using System;
using Foundation;
using WatchKit;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public bool IsEnergySavingEnabled => NSProcessInfo.ProcessInfo.LowPowerModeEnabled;

        public PowerState Status
        {
            get
            {
                switch (WKInterfaceDevice.CurrentDevice.BatteryState)
                {
                    case WKInterfaceDeviceBatteryState.Charging:
                        return PowerState.Charging;

                    case WKInterfaceDeviceBatteryState.Full:
                        return PowerState.Charged;

                    case WKInterfaceDeviceBatteryState.Unplugged:
                        return PowerState.Discharging;

                    case WKInterfaceDeviceBatteryState.Unknown:
                    default:
                        return PowerState.Unknown;
                }
            }
        }
        public int BatteryLevel => Math.Abs((int)(WKInterfaceDevice.CurrentDevice.BatteryLevel * 100F));


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                //this.timerSub = Observable
            }
            else
            {
            }
        }
    }
}
