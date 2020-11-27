using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Shiny.Power
{
    /// <summary>
    /// This service uses INotifyPropertyChanged - when you subscribe to PropertyChanged, the underlying
    /// monitors are attached
    /// </summary>
    public interface IPowerManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the current status
        /// </summary>
        PowerState Status { get; }


        /// <summary>
        /// Gets the current battery level (scaled in %/percentage - 1-100)
        /// </summary>
        int BatteryLevel { get; }


        /// <summary>
        /// Detects Android Doze or iOS Low Power mode
        /// </summary>
        bool IsEnergySavingEnabled { get; }
    }
}
