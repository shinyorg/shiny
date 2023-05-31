using System;

namespace Shiny.Power;


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
    /// Gets the current battery level (0.0-1.0)
    /// </summary>
    double Level { get; }
}
