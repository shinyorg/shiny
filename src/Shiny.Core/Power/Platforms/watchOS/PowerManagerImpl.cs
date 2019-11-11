using System;
using WatchKit;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        IDisposable? timerSub;


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
                this.timerSub?.Dispose();
            }
        }
    }
}
