using System;
using System.Collections.Generic;
using Shiny.Stores;
using Shiny.Support.Repositories.Impl;

namespace Shiny.Beacons.Infrastructure;


public class BeaconRegionRepositoryConverter : RepositoryConverter<BeaconRegion>
{
    public override BeaconRegion FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var major = this.ConvertFromStoreValue<ushort?>(values, nameof(BeaconRegion.Major));
        var minor = this.ConvertFromStoreValue<ushort?>(values, nameof(BeaconRegion.Minor));

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


    public override IEnumerable<(string Property, object Value)> ToStore(BeaconRegion entity, ISerializer serializer)
    {
        //entity.Validate();
        yield return (nameof(BeaconRegion.Uuid), entity.Uuid);
        yield return (nameof(BeaconRegion.NotifyOnEntry), entity.NotifyOnEntry);
        yield return (nameof(BeaconRegion.NotifyOnExit), entity.NotifyOnExit);

        if (entity.Major != null)
            yield return (nameof(BeaconRegion.Major), entity.Major);

        if (entity.Minor != null)
            yield return (nameof(BeaconRegion.Minor), entity.Minor);
    }
}
