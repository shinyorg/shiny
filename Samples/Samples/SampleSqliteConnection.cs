using System;
using System.IO;
using Shiny.IO;
using Samples.Models;
using SQLite;


namespace Samples
{
    public class SampleSqliteConnection : SQLiteAsyncConnection
    {
        public SampleSqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "sample.db"))
        {
            var conn = this.GetConnection();
            conn.CreateTable<BeaconEvent>();
            conn.CreateTable<GeofenceEvent>();
            conn.CreateTable<JobLog>();
            conn.CreateTable<BleEvent>();
            conn.CreateTable<GpsEvent>();
            conn.CreateTable<ErrorLog>();
            conn.CreateTable<HttpEvent>();
        }


        public AsyncTableQuery<BeaconEvent> BeaconEvents => this.Table<BeaconEvent>();
        public AsyncTableQuery<BleEvent> BleEvents => this.Table<BleEvent>();
        public AsyncTableQuery<GeofenceEvent> GeofenceEvents => this.Table<GeofenceEvent>();
        public AsyncTableQuery<JobLog> JobLogs => this.Table<JobLog>();
        public AsyncTableQuery<GpsEvent> GpsEvents => this.Table<GpsEvent>();
        public AsyncTableQuery<HttpEvent> HttpEvents => this.Table<HttpEvent>();
        public AsyncTableQuery<ErrorLog> Errors => this.Table<ErrorLog>();
    }
}
