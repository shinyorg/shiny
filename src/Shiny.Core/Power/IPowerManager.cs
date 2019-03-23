using System;
using System.ComponentModel;


namespace Shiny.Power
{
    public interface IPowerManager : INotifyPropertyChanged
    {
        PowerState Status { get; }
        int BatteryLevel { get; }
    }
}
