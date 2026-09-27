using System;

namespace Shiny.Locations;


/// <summary>
/// A circular geographic region monitored for enter, exit and (optionally) dwell transitions.
/// </summary>
/// <param name="Identifier">A unique identifier for the region.</param>
/// <param name="Center">The center point of the region.</param>
/// <param name="Radius">The radius of the region around the center.</param>
/// <param name="SingleUse">If true, the region is removed from monitoring after the first transition - or, when <see cref="DwellTime"/> is set, after the dwell fires.</param>
/// <param name="NotifyOnEntry">Whether the delegate should be invoked when the device enters the region.</param>
/// <param name="NotifyOnExit">Whether the delegate should be invoked when the device exits the region.</param>
public record GeofenceRegion(
    string Identifier,
    Position Center,
    Distance Radius,
    bool SingleUse = false,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : Shiny.Extensions.Stores.Repositories.IRepositoryEntity
{
    /// <summary>
    /// When set, <see cref="IGeofenceDelegate.OnStatusChanged"/> is invoked with <see cref="GeofenceState.Dwelling"/>
    /// once the device has stayed inside the region for this long after entering it. Leaving before then cancels it.
    /// Android supports this natively (loitering delay); iOS and Windows track it in Shiny - on iOS the app must have
    /// the <c>location</c> UIBackgroundMode to be kept awake for the dwell period, otherwise the dwell is only evaluated
    /// the next time the app runs.
    /// </summary>
    public TimeSpan? DwellTime
    {
        get;
        init
        {
            if (value is { } v && v <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(DwellTime), "DwellTime must be greater than zero");

            field = value;
        }
    }
}
