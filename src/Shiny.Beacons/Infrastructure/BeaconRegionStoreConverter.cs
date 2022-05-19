using System;
using System.Collections.Generic;
using Shiny.Stores.Infrastructure;

namespace Shiny.Beacons.Infrastructure;

public class BeaconRegionStoreConverter : IStoreConverter<BeaconRegion>
{
    public BeaconRegion FromStore(IDictionary<string, object> values) => throw new NotImplementedException();
    public IEnumerable<(string Property, object value)> ToStore(BeaconRegion entity) => throw new NotImplementedException();
}
