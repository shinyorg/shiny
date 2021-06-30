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
using Samples.Infrastructure;


namespace Samples
{
    public class AccessViewModel : ViewModel
    {
        readonly IDialogs dialogs;


        public AccessViewModel(IJobManager jobs,
                               IDialogs dialogs,
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
            this.dialogs = dialogs;

            this.Append("Jobs", jobs, () => jobs.RequestAccess());
            this.Append("Notifications", notifications, () => notifications!.RequestAccess());
            this.Append("Speech", speech, () => speech!.RequestAccess());
            this.Append("Motion Activity", activityManager, () => activityManager!.RequestAccess());
            this.Append("GPS (Background)", gps, () => gps!.RequestAccess(GpsRequest.Realtime(true)));
            this.Append("Geofences", geofences, () => geofences!.RequestAccess());
            this.Append("BluetoothLE Central", bluetooth, () => bluetooth!.RequestAccess().ToTask(CancellationToken.None));
            this.Append("iBeacons (Ranging)", beaconRanging, () => beaconRanging!.RequestAccess());
            this.Append("iBeacons (Monitoring)", beaconMonitoring, () => beaconMonitoring!.RequestAccess());
            this.Append("Push", push, async () =>
            {
                var status = await push!.RequestAccess();
                return status.Status;
            });
            this.Append("NFC", nfc, () => nfc!.RequestAccess().ToTask(CancellationToken.None));
        }


        public List<CommandItem> List { get; } = new List<CommandItem>();


        void Append(string text, object? service, Func<Task<AccessState>> request)
        {
            CommandItem item;

            if (service == null)
            {
                item = new CommandItem
                {
                    Text = text,
                    PrimaryCommand = ReactiveCommand.CreateFromTask(()
                        => this.dialogs.Snackbar($"{text} not available on this platform or is not registered")
                    )
                };
            }
            else
            {
                item = new CommandItem
                {
                    Text = text,
                    Detail = AccessState.Unknown.ToString()
                };
                item.PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var r = await request();
                    item.Detail = r.ToString();
                });
            }
            this.List.Add(item);
        }
    }
}
