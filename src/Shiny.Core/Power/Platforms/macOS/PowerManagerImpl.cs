using System;
using System.ComponentModel;

namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public bool IsEnergySavingEnabled => false;
        public PowerState Status => PowerState.Charged;
        public int BatteryLevel
        {
            get
            {
                return 100;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
