using System;

namespace Shiny.Power;


/// <summary>
/// Monitors the device's battery level and charging state.
/// </summary>
public interface IBattery
{
    /// <summary>
    /// Returns an observable that emits whenever <see cref="Status"/> or <see cref="Level"/>
    /// changes. The same instance is emitted, with updated property values.
    /// </summary>
    IObservable<IBattery> WhenChanged();

    /// <summary>
    /// Gets the current charging status.
    /// </summary>
    BatteryState Status { get; }

    /// <summary>
    /// Gets the current battery level as a fraction between 0.0 and 1.0.
    /// </summary>
    double Level { get; }
}
