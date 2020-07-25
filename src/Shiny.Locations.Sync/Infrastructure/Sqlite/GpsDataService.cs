using System;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class GpsDataService : AbstractSqliteDataService<SqliteGpsEvent, GpsEvent>, IGpsDataService
    {
        public GpsDataService(SyncSqliteConnection conn) : base(conn) {}
        protected override SqliteGpsEvent FromDomain(GpsEvent domain) => domain.ReflectCreateTo<SqliteGpsEvent>();
        protected override GpsEvent ToDomain(SqliteGpsEvent data) => data.ReflectCreateTo<GpsEvent>();
    }
}
