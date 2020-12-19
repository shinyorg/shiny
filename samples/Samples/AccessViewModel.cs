using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using Shiny;
using Shiny.Beacons;
using Shiny.BluetoothLE;
using Shiny.Jobs;
using Shiny.Locations;
using Shiny.Notifications;
using Shiny.SpeechRecognition;
using Shiny.Push;
using Shiny.Nfc;


namespace Samples
{
    public class AccessViewModel : ViewModel
    {
        public AccessViewModel(IJobManager jobs,
                               INotificationManager? notifications = null,
                               ISpeechRecognizer? speech = null,
                               IGeofenceManager? geofences = null,
                               IGpsManager? gps = null,
                               IMotionActivityManager? activityManager = null,
                               IBleManager? bluetooth = null,
                               IBeaconRangingManager? beaconRanging = null,
                               IBeaconMonitoringManager? beaconMonitoring = null,
                               IPushManager? push = null,
                               INfcManager? nfc = null)
        {
            this.Append("Jobs", AccessState.Unknown, () => jobs.RequestAccess());

            if (notifications != null)
                this.Append("Notifications", AccessState.Unknown, () => notifications.RequestAccess());

            if (speech != null)
                this.Append("Speech", AccessState.Unknown, () => speech.RequestAccess());

            if (activityManager != null)
                this.Append("Motion Activity", AccessState.Unknown, () => activityManager.RequestAccess());

            if (gps != null)
            {
                var request = new GpsRequest { UseBackground = true };
                this.Append("GPS (Background)", gps.GetCurrentStatus(request), () => gps.RequestAccess(request));
            }

            if (geofences != null)
                this.Append("Geofences", geofences.Status, () => geofences.RequestAccess());

            if (bluetooth != null)
                this.Append("BluetoothLE Central", bluetooth.Status, () => bluetooth.RequestAccess().ToTask(CancellationToken.None));

            if (beaconRanging != null)
                this.Append("iBeacons (Ranging)", AccessState.Unknown, () => beaconRanging.RequestAccess());

            if (beaconMonitoring != null)
                this.Append("iBeacons (Monitoring)", AccessState.Unknown, () => beaconMonitoring.RequestAccess());

            if (push != null)
                this.Append("Push", AccessState.Unknown, async () =>
                {
                    var status = await push.RequestAccess();
                    return status.Status;
                });

            if (nfc != null)
                this.Append("NFC", AccessState.Unknown, () => nfc.RequestAccess().ToTask(CancellationToken.None));
        }


        public List<CommandItem> List { get; } = new List<CommandItem>();


        void Append(string text, AccessState current, Func<Task<AccessState>> request)
        {
            var item = new CommandItem
            {
                Text = text,
                Detail = current.ToString()
            };
            item.PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var r = await request();
                item.Detail = r.ToString();
            });
            this.List.Add(item);
        }
    }
}
