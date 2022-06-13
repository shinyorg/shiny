using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Locations.Infrastructure;


public class GeofenceRegionStoreConverter : IStoreConverter<GeofenceRegion>
{
    const string LatitudeKey = "CenterLatitude";
    const string LongitudeKey = "CenterLongitude";


    public GeofenceRegion FromStore(IDictionary<string, object> values) => new GeofenceRegion(
        (string)values[nameof(GeofenceRegion.Identifier)],
        new Position(
            (double)values[LatitudeKey],
            (double)values[LongitudeKey]
        ),
        Distance.FromKilometers((double)values[nameof(GeofenceRegion.Radius)])
    )
    {
        NotifyOnEntry = (bool)values[nameof(GeofenceRegion.NotifyOnEntry)],
        NotifyOnExit = (bool)values[nameof(GeofenceRegion.NotifyOnExit)],
        SingleUse = (bool)values[nameof(GeofenceRegion.SingleUse)]
    };


    public IEnumerable<(string Property, object Value)> ToStore(GeofenceRegion entity)
    {
        yield return (nameof(GeofenceRegion.Identifier), entity.Identifier);
        yield return (nameof(GeofenceRegion.Radius), entity.Radius.TotalKilometers);
        yield return (LatitudeKey, entity.Center.Latitude);
        yield return (LongitudeKey, entity.Center.Longitude);
        yield return (nameof(GeofenceRegion.SingleUse), entity.SingleUse);
        yield return (nameof(GeofenceRegion.NotifyOnEntry), entity.NotifyOnEntry);
        yield return (nameof(GeofenceRegion.NotifyOnExit), entity.NotifyOnExit);
    }
}
