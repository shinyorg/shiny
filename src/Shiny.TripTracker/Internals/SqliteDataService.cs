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
            Speed = reading.Speed,
            DateCreated = reading.Timestamp
        });


        public Task<Trip> GetTrip(int tripId) => this.conn.GetAsync<Trip>(tripId);


        public async Task<IList<Trip>> GetAll() => await this.conn
            .Table<Trip>()
            .ToListAsync();


        public async Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId) => await this.conn
            .Table<TripCheckin>()
            .OrderBy(x => x.DateCreated)
            .Where(x => x.TripId == tripId)
            .ToListAsync();


        public async Task Purge() 
        {
            await this.conn.DeleteAllAsync<Trip>();
            await this.conn.DeleteAllAsync<TripCheckin>();
        }


        public async Task Remove(int tripId)
        {
            await this.conn.ExecuteAsync("DELETE FROM Trips WHERE Id = ?", tripId);
            await this.conn.ExecuteAsync("DELETE FROM TripCheckins WHERE TripId = ?", tripId);
        }


        public Task Save(Trip trip) => this.conn.InsertOrReplaceAsync(trip);
    }
}
