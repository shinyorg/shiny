using System;

namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public bool IsEnergySavingEnabled => false;
        public PowerState Status => PowerState.Unknown;
        public int BatteryLevel => -1;
    }
}
