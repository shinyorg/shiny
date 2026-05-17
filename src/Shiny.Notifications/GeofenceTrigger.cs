using System;
using Shiny.Locations;

namespace Shiny.Notifications;


/// <summary>
/// A geofence-based trigger that fires a notification when the device enters the configured region.
/// </summary>
public class GeofenceTrigger
{
    /// <summary>
    /// When true the notification fires every time the region is entered; when false it fires once and is removed.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// The geographic center of the geofence region.
    /// </summary>
    public Position? Center { get; set; }

    /// <summary>
    /// The radius around <see cref="Center"/> that defines the geofence region.
    /// </summary>
    public Distance? Radius { get; set; }


    /// <summary>
    /// Throws if <see cref="Center"/> or <see cref="Radius"/> is not set.
    /// </summary>
    public void AssertValid()
    {
        if (this.Radius == null)
            throw new InvalidOperationException("Radius is not set");

        if (this.Center == null)
            throw new InvalidOperationException("Center is not set");
    }
}
