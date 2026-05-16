using System;

namespace Shiny.Power;


public interface IBattery
{
    /// <summary>
    /// Fires when battery state changes
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the current status
    /// </summary>
    BatteryState Status { get; }

    /// <summary>
    /// Gets the current battery level (0.0-1.0)
    /// </summary>
    double Level { get; }
}
