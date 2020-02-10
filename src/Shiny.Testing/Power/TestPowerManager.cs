using System;
using Shiny.Power;


namespace Shiny.Testing.Power
{
    public class TestPowerManager : NotifyPropertyChanged, IPowerManager
    {
        public bool InPowerSavingMode { get; set; }


        PowerState powerState = PowerState.Charged;
        public PowerState Status
        {
            get => this.powerState;
            set => this.Set(ref this.powerState, value);
        }


        int level;
        public int BatteryLevel
        {
            get => this.level;
            set => this.Set(ref this.level, value);
        }
    }
}
