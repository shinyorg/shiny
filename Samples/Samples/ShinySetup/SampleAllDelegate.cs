using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny;
using Shiny.Beacons;
using Shiny.BluetoothLE.Central;
using Shiny.Locations;
using Shiny.Jobs;
using Shiny.Net.Http;
using Shiny.Notifications;
using Samples.Models;


namespace Samples.ShinySetup
{
    public class SampleAllDelegate : IGeofenceDelegate,
                                     IGpsDelegate,
                                     IBeaconDelegate,
                                     IHttpTransferDelegate,
                                     IBleStateRestoreDelegate,
                                     IJob
    {
        // notice you can inject anything you registered in your application here
        readonly SampleSqliteConnection conn;
        readonly INotificationManager notifications;


        public SampleAllDelegate(SampleSqliteConnection conn,
                                 INotificationManager notifications)
        {
            this.conn = conn;
            this.notifications = notifications;
        }


        public void OnAdvertised(IScanResult result)
        {
        }


        public void OnConnected(IPeripheral peripheral)
        {
        }


        public async void OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            await this.conn.InsertAsync(new GeofenceEvent
            {
                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Date = DateTime.Now
            });
            await this.notifications.Send(new Notification
            {
                Title = "Geofences",
                Message = $"{region.Identifier} was {newStatus}"
            });
        }


        public async void OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region)
        {
            await this.conn.InsertAsync(new BeaconEvent
            {
                Identifier = region.Identifier,
                Uuid = region.Uuid,
                Major = region.Major,
                Minor = region.Minor,
                Entered = newStatus == BeaconRegionState.Entered,
                Date = DateTime.UtcNow
            });
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            await this.notifications.Send(new Notification
            {
                Title = "Job Started",
                Message = $"{jobInfo.Identifier} started"
            });
            var loops = jobInfo.Parameters.Get("Loops", 10);
            for (var i = 0; i < loops; i++)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                await Task.Delay(1000, cancelToken).ConfigureAwait(false);
            }
            await this.notifications.Send(new Notification
            {
                Title = "Job Finished",
                Message = $"{jobInfo.Identifier} Finished"
            });

            // you really shouldn't lie about this on iOS as it is watching :)
            return true;
        }


        public async void OnReading(IGpsReading reading)
        {
            await this.conn.InsertAsync(new GpsEvent
            {
                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Altitude = reading.Altitude,
                PositionAccuracy = reading.PositionAccuracy,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                Date = reading.Timestamp.ToLocalTime()
            });
        }

        public void OnStatusChanged(IHttpTransfer transfer)
        {
        }


        public void OnError(IHttpTransfer transfer, Exception ex)
        {
        }


        public void OnCompleted(IHttpTransfer transfer)
        {
        }
    }
}
