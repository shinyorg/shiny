using System;

namespace Shiny.Power;


/// <summary>
/// Provides access to the device battery level and charging state.
/// </summary>
public interface IBattery
{
    /// <summary>
    /// Fires when the battery status or level changes.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the current battery charging status.
    /// </summary>
    BatteryState Status { get; }

    /// <summary>
    /// Gets the current battery charge level as a value between 0.0 and 1.0.
    /// </summary>
    double Level { get; }
}
