using System;
using Shiny.Infrastructure;


namespace Shiny.Locations.Infrastructure
{
    internal static class InternalExtensions
    {
        internal static RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> Wrap(this IRepository repository) => new RepositoryWrapper<GeofenceRegion, GeofenceRegionStore>
        (
            repository,
            args => new GeofenceRegionStore
            {
                Identifier = args.Identifier,
                CenterLatitude = args.Center.Latitude,
                CenterLongitude = args.Center.Longitude,
                RadiusMeters = args.Radius.TotalMeters,
                SingleUse = args.SingleUse,
                NotifyOnEntry = args.NotifyOnEntry,
                NotifyOnExit = args.NotifyOnExit
            },
            store => new GeofenceRegion(
                store.Identifier,
                new Position(store.CenterLatitude, store.CenterLongitude),
                Distance.FromMeters(store.RadiusMeters)
            )
            {
                SingleUse = store.SingleUse,
                NotifyOnEntry = store.NotifyOnEntry,
                NotifyOnExit = store.NotifyOnExit
            }
        );
    }
}
