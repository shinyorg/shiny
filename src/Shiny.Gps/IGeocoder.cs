using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Converts coordinates into human-readable addresses using the platform geocoder (MapKit / CoreLocation on iOS and
/// Mac Catalyst, <c>android.location.Geocoder</c> on Android) or, everywhere else, <see cref="NominatimGeocoder"/>
/// against OpenStreetMap. All of them need network access.
/// </summary>
public interface IGeocoder
{
    /// <summary>
    /// Whether a geocoder is available. The native Android geocoder reports false on devices without a geocoding
    /// backend (typically no Google Play Services) - <c>AddGeocoding()</c> registers <see cref="NominatimGeocoder"/>
    /// on those devices instead, so through it this is always true.
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
