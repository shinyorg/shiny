using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.IO;
using SQLite;


namespace Shiny.TripTracker.Internals
{
    public class SqliteDataService : IDataService
    {
        readonly SQLiteAsyncConnection conn;


        public SqliteDataService(IFileSystem fileSystem)
        {
            this.conn = new SQLiteAsyncConnection(Path.Combine(fileSystem.AppData.FullName, "shinytrip.db"));
        }

        public Task Checkin(TripCheckin checkin)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Trip>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IList<TripCheckin>> GetCheckinsByTrip(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task Purge()
        {
            throw new NotImplementedException();
        }

        public Task Remove(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task Save(Trip trip)
        {
            throw new NotImplementedException();
        }
    }
}
