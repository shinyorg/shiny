using System;
using SQLite;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class SqliteGpsEvent : GpsEvent
    {
        [PrimaryKey]
        public new string Id { get; set; }
    }
}
