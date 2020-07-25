using System;
using SQLite;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class SqliteGeofenceEvent : GeofenceEvent
    {
        [PrimaryKey]
        public string Id { get; set; }
    }
}
