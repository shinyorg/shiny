using System;
using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Locations.Infrastructure;


public class GeofenceRegionStoreConverter : IStoreConverter<GeofenceRegion>
{
    const string LatitudeKey = "CenterLatitude";
    const string LongitudeKey = "CenterLongitude";


    public GeofenceRegion FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var id = (string)values[nameof(GeofenceRegion.Identifier)];
        var lat = (double)values[LatitudeKey];
        var lng = (double)values[LongitudeKey];
        var radiusKm = Convert.ToDouble(values[nameof(GeofenceRegion.Radius)]);
        var dist = Distance.FromKilometers(radiusKm);

        var region = new GeofenceRegion(id, new Position(lat, lng), dist);
        region.NotifyOnEntry = (bool)values[nameof(GeofenceRegion.NotifyOnEntry)];
        region.NotifyOnExit = (bool)values[nameof(GeofenceRegion.NotifyOnExit)];
        region.SingleUse = (bool)values[nameof(GeofenceRegion.SingleUse)];

        return region;
    }

    public IEnumerable<(string Property, object Value)> ToStore(GeofenceRegion entity, ISerializer serializer)
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
