using System;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        public PowerState Status => throw new NotImplementedException();

        public int BatteryLevel => throw new NotImplementedException();
    }
}
