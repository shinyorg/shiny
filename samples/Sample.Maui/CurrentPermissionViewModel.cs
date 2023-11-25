using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using Shiny.Locations;

namespace Sample;


public class CurrentPermissionViewModel : ViewModel
{
    public CurrentPermissionViewModel(
        IBleManager bleManager,
        IBleHostingManager bleHostingManager,
        IGpsManager gpsManager,
        IGeofenceManager geofenceManager,
        BaseServices services
    ) : base(services)
    {
        this.Refresh = ReactiveCommand.Create(() =>
        {
            this.BleClient = bleManager.CurrentAccess;
            this.BleHostAdvertised = bleHostingManager.AdvertisingAccessStatus;
            this.BleHostGatt = bleHostingManager.GattAccessStatus;
            this.GpsInApp = gpsManager.GetCurrentStatus(GpsRequest.Foreground);
            this.GpsBackground = gpsManager.GetCurrentStatus(GpsRequest.Realtime(false));
            this.Geofencing = geofenceManager.CurrentStatus;
        });
    }


    public ICommand Refresh { get; }
    [Reactive] public AccessState BleClient { get; private set; }
    [Reactive] public AccessState BleHostGatt { get; private set; }
    [Reactive] public AccessState BleHostAdvertised { get; private set; }
    [Reactive] public AccessState GpsInApp { get; private set; }
    [Reactive] public AccessState GpsBackground { get; private set; }
    [Reactive] public AccessState Geofencing { get; private set; }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Refresh.Execute(null);
    }
}