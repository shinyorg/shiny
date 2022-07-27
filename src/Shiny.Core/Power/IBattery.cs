using System;

namespace Shiny.Power;


/// <summary>
/// This service uses INotifyPropertyChanged - when you subscribe to PropertyChanged, the underlying
/// monitors are attached
/// </summary>
public interface IBattery
{
    /// <summary>
    /// Monitor changes
    /// </summary>
    /// <returns></returns>
    IObservable<IBattery> WhenChanged();

    /// <summary>
    /// Gets the current status
    /// </summary>
    BatteryState Status { get; }

    /// <summary>
    /// Gets the current battery level (scaled in %/percentage - 1-100)
    /// </summary>
    int BatteryLevel { get; }

    /// <summary>
    /// Detects Android Doze or iOS Low Power mode
    /// </summary>
    State IsEnergySavingEnabled { get; }
}
