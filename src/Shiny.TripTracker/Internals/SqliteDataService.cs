using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.IO;
using Shiny.Locations;
using SQLite;


namespace Shiny.TripTracker.Internals
{
    public class SqliteDataService : IDataService
    {
        readonly SQLiteAsyncConnection conn;


        public SqliteDataService(IFileSystem fileSystem)
        {
            this.conn = new SQLiteAsyncConnection(Path.Combine(fileSystem.AppData.FullName, "shinytrip.db"));

            var sync = this.conn.GetConnection();
            sync.CreateTable<Trip>();
            sync.CreateTable<TripCheckin>();
        }


        public Task Checkin(int tripId, IGpsReading reading) => this.conn.InsertAsync(new TripCheckin
        {
            TripId = tripId,
            Latitude = reading.Position.Latitude,
            Longitude = reading.Position.Longitude,
            Direction = reading.Heading,
            Speed = reading.Speed,
            DateCreated = reading.Timestamp
        });


        public Task<Trip> GetTrip(int tripId) => this.conn.GetAsync<Trip>(tripId);


        public async Task<IList<Trip>> GetAll() => await this.conn
            .Table<Trip>()
            .OrderBy(x => x.DateFinished)
            .ToListAsync();


        public Task<double> GetTripAverageSpeedInMetersPerHour(int tripId)
            => this.conn.ExecuteScalarAsync<double>(
                $"SELECT AVG({nameof(TripCheckin.Speed)}) FROM {nameof(TripCheckin)} WHERE {nameof(TripCheckin.TripId)} = ?",
                tripId
            );


        public async Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId) => await this.conn
            .Table<TripCheckin>()
            .OrderBy(x => x.DateCreated)
            .Where(x => x.TripId == tripId)
            .ToListAsync();


        public async Task<double> GetTripTotalDistanceInMeters(int tripId)
        {
            var total = 0d;
            Position? last = null;
            var checkins = await this.GetCheckinsByTrip(tripId);

            foreach (var checkin in checkins)
            {
                var current = new Position(checkin.Latitude, checkin.Longitude);
                if (last != null)
                    total += last.GetDistanceTo(current).TotalMeters;

                last = current;
            }
            return total;
        }


        public async Task Purge()
        {
            await this.conn.DeleteAllAsync<Trip>();
            await this.conn.DeleteAllAsync<TripCheckin>();
        }


        public async Task Remove(int tripId)
        {
            await this.conn.Table<TripCheckin>().DeleteAsync(x => x.TripId == tripId);
            await this.conn.Table<Trip>().DeleteAsync(x => x.Id == tripId);
        }


        public Task Save(Trip trip) => this.conn.InsertOrReplaceAsync(trip);
    }
}
