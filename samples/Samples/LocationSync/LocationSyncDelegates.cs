using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;
using Shiny.Locations.Sync;
using GeofenceEvent = Shiny.Locations.Sync.GeofenceEvent;
using GpsEvent = Shiny.Locations.Sync.GpsEvent;


namespace Samples.ShinyDelegates
{
    public class LocationSyncDelegates : ReactiveObject, IGeofenceSyncDelegate, IGpsSyncDelegate
    {
        readonly SampleSqliteConnection conn;
        public LocationSyncDelegates(SampleSqliteConnection conn) => this.conn = conn;


        [Reactive] public bool IsSyncDelegateCrashEnabled { get; set; }


        public async Task Process(IEnumerable<GpsEvent> events, CancellationToken cancelToken) 
        {
            foreach (var gpsEvent in events)
            { 
                await this.DoProcess(
                    gpsEvent.Id, 
                    $"GPS - Lat: {gpsEvent.Latitude} Lng: {gpsEvent.Longitude}"
                );
            }
        }


        public async Task Process(IEnumerable<GeofenceEvent> events, CancellationToken cancelToken) 
        {
            foreach (var geofence in events)
            { 
                await this.DoProcess(
                    geofence.Id, 
                    $"Geofence: {geofence.Identifier} (Entered: {geofence.Entered})"
                );
            }
        }


        async Task DoProcess(string identifier, string desc)
        {
            var e = await this.conn.FindAsync<LocationSyncEvent>(identifier);
            if (e == null)
            {
                e = new LocationSyncEvent
                {
                    Id = identifier,
                    Description = desc,
                    DateLastAttempt = DateTime.UtcNow,
                    DateCreated = DateTime.UtcNow                    
                };
            }
            else if (this.IsSyncDelegateCrashEnabled)
            { 
                e.Retries++;
                e.DateLastAttempt = DateTime.UtcNow;
            }

            if (!this.IsSyncDelegateCrashEnabled)
                e.DateSync = DateTime.UtcNow;

            await this.conn.InsertOrReplaceAsync(e);
            if (this.IsSyncDelegateCrashEnabled)
                throw new ArgumentException("No processing events right now");
        }
    }
}
