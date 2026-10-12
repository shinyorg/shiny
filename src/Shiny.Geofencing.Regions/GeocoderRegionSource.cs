using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// An <see cref="IRegionSource"/> that resolves regions by reverse geocoding the position through
/// <see cref="IGeocoder"/> - no region data to store or seed, but every evaluated reading needs network access.
/// The matched region object is the <see cref="Placemark"/>.
/// <para>
/// A failed lookup (no connectivity, a throttled request) throws, which the monitor treats as "unknown" - the
/// set keeps its current region rather than raising a false exit. Geocoder sets evaluating the same reading
/// share one lookup.
/// </para>
/// </summary>
public class GeocoderRegionSource : IRegionSource
{
    // one lookup per geocoder per position - three geocoder sets (country, state, city) cost one request a reading
    static readonly ConditionalWeakTable<IGeocoder, LookupCache> caches = new();

    readonly IGeocoder geocoder;
    readonly Func<Placemark, string?> idSelector;
    readonly Func<Placemark, string?> nameSelector;


    /// <summary>Creates a source that matches at one address level.</summary>
    /// <param name="geocoder">The geocoder.</param>
    /// <param name="level">Which part of the address is the region.</param>
    public GeocoderRegionSource(IGeocoder geocoder, GeocoderRegionLevel level)
        : this(geocoder, GetIdSelector(level), GetNameSelector(level)) { }


    /// <summary>Creates a source with custom region selection.</summary>
    /// <param name="geocoder">The geocoder.</param>
    /// <param name="idSelector">
    /// Returns the region id for a placemark, or null when the placemark doesn't resolve to a region. The id is
    /// persisted as the current region, so it must be stable for the same place.
    /// </param>
    /// <param name="nameSelector">Optional display name echoed on change events. Defaults to the id.</param>
    public GeocoderRegionSource(IGeocoder geocoder, Func<Placemark, string?> idSelector, Func<Placemark, string?>? nameSelector = null)
    {
        ArgumentNullException.ThrowIfNull(geocoder);
        ArgumentNullException.ThrowIfNull(idSelector);

        this.geocoder = geocoder;
        this.idSelector = idSelector;
        this.nameSelector = nameSelector ?? idSelector;
    }


    /// <inheritdoc />
    public async Task<RegionMatch?> FindRegion(Position position, CancellationToken cancellationToken = default)
    {
        var placemarks = await caches
            .GetValue(this.geocoder, _ => new LookupCache())
            .Get(this.geocoder, position, cancellationToken)
            .ConfigureAwait(false);

        var match = placemarks
            .Select(x => (Placemark: x, Id: this.idSelector(x)))
            .FirstOrDefault(x => !String.IsNullOrWhiteSpace(x.Id));

        return match.Id == null
            ? null
            : new RegionMatch(match.Id, this.nameSelector(match.Placemark), match.Placemark);
    }


    /// <summary>
    /// Always null - a geocoder cannot look a region up by id. An exit raised after the app was relaunched
    /// therefore carries the region id only (no name or placemark).
    /// </summary>
    public Task<RegionMatch?> GetRegion(string regionId, CancellationToken cancellationToken = default)
        => Task.FromResult<RegionMatch?>(null);


    static Func<Placemark, string?> GetIdSelector(GeocoderRegionLevel level) => level switch
    {
        GeocoderRegionLevel.Country => x => x.CountryCode,
        GeocoderRegionLevel.AdministrativeArea => x => Join(x.CountryCode, x.AdministrativeArea),
        GeocoderRegionLevel.SubAdministrativeArea => x => Join(x.CountryCode, x.AdministrativeArea, x.SubAdministrativeArea),
        GeocoderRegionLevel.Locality => x => Join(x.CountryCode, x.AdministrativeArea, x.Locality),
        GeocoderRegionLevel.SubLocality => x => Join(x.CountryCode, x.AdministrativeArea, x.Locality, x.SubLocality),
        GeocoderRegionLevel.PostalCode => x => Join(x.CountryCode, x.PostalCode),
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };


    static Func<Placemark, string?> GetNameSelector(GeocoderRegionLevel level) => level switch
    {
        GeocoderRegionLevel.Country => x => x.CountryName ?? x.CountryCode,
        GeocoderRegionLevel.AdministrativeArea => x => x.AdministrativeArea,
        GeocoderRegionLevel.SubAdministrativeArea => x => x.SubAdministrativeArea,
        GeocoderRegionLevel.Locality => x => x.Locality,
        GeocoderRegionLevel.SubLocality => x => x.SubLocality,
        GeocoderRegionLevel.PostalCode => x => x.PostalCode,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };


    // every part is required - a placemark missing the leaf (no locality out in the country) is "outside"
    // rather than a half-built id that would later look like a different region
    static string? Join(params string?[] parts)
        => parts.Any(String.IsNullOrWhiteSpace) ? null : String.Join("-", parts);


    sealed class LookupCache
    {
        readonly object syncLock = new();
        Position? position;
        Task<IReadOnlyList<Placemark>>? lookup;


        public Task<IReadOnlyList<Placemark>> Get(IGeocoder geocoder, Position position, CancellationToken cancellationToken)
        {
            lock (this.syncLock)
            {
                var reuse = this.lookup != null
                    && position.Equals(this.position)
                    && !this.lookup.IsFaulted
                    && !this.lookup.IsCanceled;

                if (!reuse)
                {
                    this.position = position;
                    this.lookup = geocoder.ReverseGeocode(position, cancellationToken);
                }
                return this.lookup!;
            }
        }
    }
}
