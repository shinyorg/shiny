using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Locations;


/// <summary>
/// Configures which region sets are monitored and how aggressively GPS readings are processed.
/// </summary>
public class RegionGeofenceConfig
{
    readonly List<RegionSet> regionSets = new();


    /// <summary>
    /// The GPS listener configuration. Defaults to realtime background tracking - the reading filters below
    /// are what keep the work (and the battery cost of processing) down, not the listener frequency.
    /// </summary>
    public GpsRequest GpsRequest { get; set; } = new(GpsBackgroundMode.Realtime);

    /// <summary>Minimum distance the device must move before a reading is evaluated. Default 300m.</summary>
    public Distance? MinimumDistance { get; set; } = Distance.FromMeters(300);

    /// <summary>Minimum time between evaluated readings. Default 1 minute.</summary>
    public TimeSpan? MinimumTime { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Safety net - once the device has moved this far a reading is always evaluated, even if
    /// <see cref="MinimumTime"/> hasn't elapsed. Unset by default.
    /// </summary>
    public Distance? MaximumDistance { get; set; }

    /// <summary>
    /// Safety net - once this much time has passed a reading is always evaluated, even if
    /// <see cref="MinimumDistance"/> hasn't been met. Unset by default.
    /// </summary>
    public TimeSpan? MaximumTime { get; set; }

    /// <summary>The registered region sets.</summary>
    public IReadOnlyList<RegionSet> RegionSets => this.regionSets;


    /// <summary>
    /// Registers a layer of geofence regions. The monitor tracks the device's current region independently
    /// per set, so a device can be inside one region of each set at the same time.
    /// </summary>
    /// <param name="name">A stable name for the set - echoed on every change and used as the persistence key.</param>
    /// <param name="sourceFactory">Creates the set's region source from the container.</param>
    public RegionGeofenceConfig AddRegionSet(string name, Func<IServiceProvider, IRegionSource> sourceFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sourceFactory);

        if (this.regionSets.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A region set named '{name}' is already registered.", nameof(name));

        this.regionSets.Add(new RegionSet(name, sourceFactory));
        return this;
    }


    /// <summary>
    /// Registers a layer of regions resolved by reverse geocoding (<see cref="IGeocoder"/>) at one address
    /// level - country, state/province, city and so on. Needs no region data, but needs network access for
    /// every evaluated reading. <c>AddRegionGeofencing</c> registers the geocoder if it isn't already.
    /// </summary>
    /// <param name="name">A stable name for the set - echoed on every change and used as the persistence key.</param>
    /// <param name="level">Which part of the address is the region.</param>
    public RegionGeofenceConfig AddGeocoderRegionSet(string name, GeocoderRegionLevel level)
    {
        this.UsesGeocoder = true;
        return this.AddRegionSet(name, sp => new GeocoderRegionSource(sp.GetRequiredService<IGeocoder>(), level));
    }


    /// <summary>
    /// Registers a layer of regions resolved by reverse geocoding (<see cref="IGeocoder"/>) with custom
    /// region selection. <c>AddRegionGeofencing</c> registers the geocoder if it isn't already.
    /// </summary>
    /// <param name="name">A stable name for the set - echoed on every change and used as the persistence key.</param>
    /// <param name="idSelector">
    /// Returns the region id for a placemark, or null when it doesn't resolve to a region. Must be stable for the same place.
    /// </param>
    /// <param name="nameSelector">Optional display name echoed on change events. Defaults to the id.</param>
    public RegionGeofenceConfig AddGeocoderRegionSet(string name, Func<Placemark, string?> idSelector, Func<Placemark, string?>? nameSelector = null)
    {
        ArgumentNullException.ThrowIfNull(idSelector);
        this.UsesGeocoder = true;
        return this.AddRegionSet(name, sp => new GeocoderRegionSource(sp.GetRequiredService<IGeocoder>(), idSelector, nameSelector));
    }


    /// <summary>Whether a geocoder region set was registered, so registration has to bring in <see cref="IGeocoder"/>.</summary>
    internal bool UsesGeocoder { get; private set; }


    /// <summary>
    /// Registers a layer of geofence regions backed by an existing source instance.
    /// </summary>
    /// <param name="name">A stable name for the set - echoed on every change and used as the persistence key.</param>
    /// <param name="source">The set's region source.</param>
    public RegionGeofenceConfig AddRegionSet(string name, IRegionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return this.AddRegionSet(name, _ => source);
    }
}
