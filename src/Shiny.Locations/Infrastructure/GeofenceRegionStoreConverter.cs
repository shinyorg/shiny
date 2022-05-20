using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Locations.Infrastructure;

public class GeofenceRegionStoreConverter : IStoreConverter<GeofenceRegion>
{
    public GeofenceRegion FromStore(IDictionary<string, object> values) => throw new System.NotImplementedException();
    public IEnumerable<(string Property, object value)> ToStore(GeofenceRegion entity) => throw new System.NotImplementedException();
}
