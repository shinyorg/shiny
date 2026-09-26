using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Converts coordinates into human-readable addresses using the platform geocoder
/// (MapKit / CoreLocation on Apple, <c>android.location.Geocoder</c> on Android). Both need network access.
/// </summary>
public interface IGeocoder
{
    /// <summary>
    /// Whether the platform has a geocoder available. On Android this is false on devices without a geocoding
    /// backend (typically no Google Play Services).
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Looks up the addresses for a position, most relevant first.
    /// </summary>
    /// <param name="position">The position to look up.</param>
    /// <param name="cancelToken">Cancels the pending request.</param>
    /// <returns>The matching placemarks - empty if nothing was found.</returns>
    Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default);
}
