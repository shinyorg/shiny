using System;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class GeofenceDataService : AbstractSqliteDataService<SqliteGeofenceEvent, GeofenceEvent>, IGeofenceDataService
    {
        public GeofenceDataService(SyncSqliteConnection conn) : base(conn) {}
        protected override SqliteGeofenceEvent FromDomain(GeofenceEvent domain) => domain.ReflectCreateTo<SqliteGeofenceEvent>();
        protected override GeofenceEvent ToDomain(SqliteGeofenceEvent data) => data.ReflectCreateTo<GeofenceEvent>();
    }
}
