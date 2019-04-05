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
using Samples.Settings;


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
        readonly AppSettings appSettings;


        public SampleAllDelegate(SampleSqliteConnection conn,
                                 AppSettings appSettings,
                                 INotificationManager notifications)
        {
            this.conn = conn;
            this.appSettings = appSettings;
            this.notifications = notifications;
        }


        public void OnAdvertised(IScanResult result)
        {
        }


        public void OnConnected(IPeripheral peripheral)
        {
            //await this.DoNotification(
            //    "BluetoothLE Device Connected",
            //    $"{region.Identifier} was {newStatus}",
            //    this.appSettings.UseBleNotifications
            //);
        }


        public async void OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            await this.conn.InsertAsync(new GeofenceEvent
            {
                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Date = DateTime.Now
            });
            await this.DoNotification(
                "Geofence Event",
                $"{region.Identifier} was {newStatus}",
                this.appSettings.UseNotificationsGeofences
            );
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
            await this.DoNotification
            (
                "Beacon Region {newStatus}",
                $"{region.Identifier} - {region.Uuid}/{region.Major}/{region.Minor}",
                this.appSettings.UseNotificationsBeaconMonitoring
            );
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            await this.DoNotification(
                "Job Started",
                $"{jobInfo.Identifier} Started",
                this.appSettings.UseNotificationsJobs
            );
            var loops = jobInfo.Parameters.Get("Loops", 10);
            for (var i = 0; i < loops; i++)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                await Task.Delay(1000, cancelToken).ConfigureAwait(false);
            }
            await this.DoNotification(
                "Job Finished",
                $"{jobInfo.Identifier} Finished",
                this.appSettings.UseNotificationsJobs
            );

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


        public async void OnError(IHttpTransfer transfer, Exception ex)
            => await this.CreateHttpTransferEvent(transfer, "ERROR: " + ex);


        public async void OnCompleted(IHttpTransfer transfer)
            => await this.CreateHttpTransferEvent(transfer, "COMPLETE");


        async Task CreateHttpTransferEvent(IHttpTransfer transfer, string description)
        {
            await this.conn.InsertAsync(new HttpEvent
            {
                Identifier = transfer.Identifier,
                IsUpload = transfer.Request.IsUpload,
                FileSize = transfer.FileSize,
                Uri = transfer.Request.Uri,
                Description = description,
                DateCreated = DateTime.Now
            });
            await this.DoNotification("HTTP Transfer", description, appSettings.UseNotificationsHttpTransfers);
        }


        async Task DoNotification(string title, string message, bool enabled)
        {
            if (enabled)
                await this.notifications.Send(new Notification { Title = title, Message = message });
        }
    }
}
