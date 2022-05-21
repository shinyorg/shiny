using System;
using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Beacons.Infrastructure;


public class BeaconRegionStoreConverter : IStoreConverter<BeaconRegion>
{
    public BeaconRegion FromStore(IDictionary<string, object> values)
    {
        ushort? major = null;
        ushort? minor = null;

        if (values.ContainsKey(nameof(BeaconRegion.Major)))
            major = (ushort)values[nameof(BeaconRegion.Major)];

        if (values.ContainsKey(nameof(BeaconRegion.Minor)))
            major = (ushort)values[nameof(BeaconRegion.Minor)];

        return new BeaconRegion(
            (string)values[nameof(BeaconRegion.Identifier)],
            Guid.Parse((string)values[nameof(BeaconRegion.Uuid)]),
            major,
            minor
        )
        {
            NotifyOnEntry = (bool)values[nameof(BeaconRegion.NotifyOnEntry)],
            NotifyOnExit = (bool)values[nameof(BeaconRegion.NotifyOnExit)]
        };
    }


    public IEnumerable<(string Property, object value)> ToStore(BeaconRegion entity)
    {
        entity.Validate();

        yield return (nameof(BeaconRegion.Identifier), entity.Identifier);
        yield return (nameof(BeaconRegion.Uuid), entity.Uuid);
        yield return (nameof(BeaconRegion.NotifyOnEntry), entity.NotifyOnEntry);
        yield return (nameof(BeaconRegion.NotifyOnExit), entity.NotifyOnExit);

        if (entity.Major != null)
            yield return (nameof(BeaconRegion.Major), entity.Major);

        if (entity.Minor != null)
            yield return (nameof(BeaconRegion.Minor), entity.Minor);
    }
}
