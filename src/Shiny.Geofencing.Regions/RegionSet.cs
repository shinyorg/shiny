using System;

namespace Shiny.Locations;


/// <summary>
/// A monitored layer of geofence regions (states, cities, delivery zones) backed by one
/// <see cref="IRegionSource"/>. Created by <see cref="RegionGeofenceConfig.AddRegionSet(string, Func{IServiceProvider, IRegionSource})"/>.
/// </summary>
public sealed class RegionSet
{
    internal RegionSet(string name, Func<IServiceProvider, IRegionSource> sourceFactory)
    {
        this.Name = name;
        this.SourceFactory = sourceFactory;
    }


    /// <summary>The name this set was registered under - echoed on every change and used as the persistence key.</summary>
    public string Name { get; }

    /// <summary>Creates the set's source from the container. Invoked once, when the evaluator is created.</summary>
    internal Func<IServiceProvider, IRegionSource> SourceFactory { get; }
}
