using System;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Prism.Navigation;
using Shiny;
using Shiny.Locations;
using Samples.Infrastructure;


namespace Samples.Geofences
{
    public class CreateViewModel : ViewModel
    {
        const int DEFAULT_DISTANCE_METERS = 200;
        readonly IGeofenceManager geofenceManager;
        readonly IGpsManager gpsManager;
        readonly INavigationService navigator;
        readonly IDialogs dialogs;


        public CreateViewModel(INavigationService navigator,
                               IGeofenceManager geofenceManager,
                               IGpsManager gpsManager,
                               IDialogs dialogs)
        {
            this.navigator = navigator;
            this.geofenceManager = geofenceManager;
            this.gpsManager = gpsManager;
            this.dialogs = dialogs;

            var hasEventType = this.WhenAny(
                x => x.NotifyOnEntry,
                x => x.NotifyOnExit,
                (entry, exit) => entry.GetValue() || exit.GetValue()
            );

            //geofenceManager
            //    .WhenAccessStatusChanged()
            //    .ToPropertyEx(this, x => x.AccessStatus);
            this.AccessStatus = geofenceManager.Status;

            this.SetCurrentLocation = ReactiveCommand.CreateFromTask(async ct =>
            {
                var loc = await this.gpsManager.GetLastReading().ToTask(ct);
                this.CenterLatitude = loc?.Position?.Latitude ?? 0;
                this.CenterLongitude = loc?.Position?.Longitude ?? 0;
            });
            this.BindBusyCommand(this.SetCurrentLocation);

            this.RequestAccess = ReactiveCommand.CreateFromTask(
                async () => this.AccessStatus = await geofenceManager.RequestAccess()
            );

            this.AddCnTower = ReactiveCommand.CreateFromTask(
                _ => this.AddGeofence(
                    "CNTowerToronto",
                    43.6425662,
                    -79.3892508,
                    DEFAULT_DISTANCE_METERS
                ),
                hasEventType
            );

            this.AddAppleHQ = ReactiveCommand.CreateFromTask(
                _ => this.AddGeofence(
                    "AppleHQ",
                    37.3320045,
                    -122.0329699,
                    DEFAULT_DISTANCE_METERS
                ),
                hasEventType
            );

            this.CreateGeofence = ReactiveCommand.CreateFromTask(
                _ => this.AddGeofence
                (
                    this.Identifier,
                    this.CenterLatitude.Value,
                    this.CenterLongitude.Value,
                    this.RadiusMeters
                ),
                this.WhenAny(
                    x => x.AccessStatus,
                    x => x.Identifier,
                    x => x.RadiusMeters,
                    x => x.CenterLatitude,
                    x => x.CenterLongitude,
                    x => x.NotifyOnEntry,
                    x => x.NotifyOnExit,
                    (access, id, rad, lat, lng, entry, exit) =>
                    {
                        if (access.GetValue() != AccessState.Available)
                            return false;

                        if (id.GetValue().IsEmpty())
                            return false;

                        var radius = rad.GetValue();
                        if (radius < 200 || radius > 5000)
                            return false;

                        var latv = lat.GetValue();
                        if (latv != null && (latv >= 89.9 || latv <= -89.9))
                            return false;

                        var lngv = lng.GetValue();
                        if (lngv != null && (lngv >= 179.9 || lngv <= -179.9))
                            return false;

                        if (!entry.GetValue() && !exit.GetValue())
                            return false;

                        return true;
                    }
                )
            );
        }


        public ICommand RequestAccess { get; }
        public ICommand CreateGeofence { get; }
        public ICommand SetCurrentLocation { get; }
        public ICommand AddCnTower { get; }
        public ICommand AddAppleHQ { get; }

        //public AccessState AccessStatus { [ObservableAsProperty] get; }
        [Reactive] public AccessState AccessStatus { get; private set; }
        [Reactive] public string Identifier { get; set; }
        [Reactive] public double? CenterLatitude { get; set; }
        [Reactive] public double? CenterLongitude { get; set; }
        [Reactive] public int RadiusMeters { get; set; } = DEFAULT_DISTANCE_METERS;
        [Reactive] public bool SingleUse { get; set; }
        [Reactive] public bool NotifyOnEntry { get; set; } = true;
        [Reactive] public bool NotifyOnExit { get; set; } = true;


        async Task AddGeofence(string id, double lat, double lng, double distance)
        {
            var access = await this.dialogs.RequestAccess(this.geofenceManager.RequestAccess);
            if (access)
            {
                await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                    id,
                    new Position(lat, lng),
                    Distance.FromMeters(distance)
                )
                {
                    NotifyOnEntry = this.NotifyOnEntry,
                    NotifyOnExit = this.NotifyOnExit,
                    SingleUse = this.SingleUse
                });
                await this.navigator.GoBack();
            }
        }
    }
}
