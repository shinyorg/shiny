using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny;
using Shiny.Beacons;
using Shiny.BluetoothLE.Central;
using Shiny.Jobs;
using Shiny.Locations;
using Shiny.Notifications;
using Shiny.SpeechRecognition;
using ReactiveUI;


namespace Samples
{
    public class AccessViewModel : ViewModel
    {
        public AccessViewModel(IJobManager jobs,
                               INotificationManager notifications = null,
                               ISpeechRecognizer speech = null,
                               IGeofenceManager geofences = null,
                               IGpsManager gps = null,
                               ICentralManager bluetooth = null,
                               IBeaconManager beacons = null)
        {
            this.Append("Jobs", AccessState.Unknown, () => jobs.RequestAccess());

            if (notifications != null)
                this.Append("Notifications", AccessState.Unknown, () => notifications.RequestAccess());

            if (speech != null)
                this.Append("Speech", AccessState.Unknown, () => speech.RequestAccess().ToTask(CancellationToken.None));

            if (gps != null)
                this.Append("GPS", gps.Status, () => gps.RequestAccess(true));

            if (geofences != null)
                this.Append("Geofences", geofences.Status, () => geofences.RequestAccess());

            if (bluetooth != null)
                this.Append("BluetoothLE Central", bluetooth.Status, () => bluetooth.RequestAccess().ToTask(CancellationToken.None));

            if (beacons != null)
                this.Append("iBeacons", beacons.Status, () => beacons.RequestAccess(true));
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
