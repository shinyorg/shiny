using System;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class GeofenceDataService : AbstractSqliteDataService<SqliteGeofenceEvent, GeofenceEvent>, IGeofenceDataService
    {
        public GeofenceDataService(SyncSqliteConnection conn) : base(conn) {}
        protected override SqliteGeofenceEvent FromDomain(GeofenceEvent x) => new SqliteGeofenceEvent
        {
            Activities = x.Activities,
            DateCreated = x.DateCreated,
            Entered = x.Entered,
            Id = x.Id,
            Identifier = x.Identifier
        };

        protected override GeofenceEvent ToDomain(SqliteGeofenceEvent x) => new GeofenceEvent
        {
            Activities = x.Activities,
            DateCreated = x.DateCreated,
            Entered = x.Entered,
            Id = x.Id,
            Identifier = x.Identifier
        };
    }
}
