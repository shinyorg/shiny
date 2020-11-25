using System;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class GpsDataService : AbstractSqliteDataService<SqliteGpsEvent, GpsEvent>, IGpsDataService
    {
        public GpsDataService(SyncSqliteConnection conn) : base(conn) {}
        protected override SqliteGpsEvent FromDomain(GpsEvent x) => new SqliteGpsEvent
        {
            Activities = x.Activities,
            Altitude = x.Altitude,
            DateCreated = x.DateCreated,
            Heading = x.Heading,
            HeadingAccuracy = x.HeadingAccuracy,
            Id = x.Id,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            PositionAccuracy = x.PositionAccuracy,
            Speed = x.Speed
        };


        protected override GpsEvent ToDomain(SqliteGpsEvent x) => new GpsEvent
        {
            Activities = x.Activities,
            Altitude = x.Altitude,
            DateCreated = x.DateCreated,
            Heading = x.Heading,
            HeadingAccuracy = x.HeadingAccuracy,
            Id = x.Id,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            PositionAccuracy = x.PositionAccuracy,
            Speed = x.Speed
        };
    }
}
