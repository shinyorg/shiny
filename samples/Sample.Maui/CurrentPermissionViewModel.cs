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
            this.IsRefreshing = true;
            this.PermissionSet = new List<PermissionViewModel> {
                new PermissionViewModel(
                    "BLE Client",
                    () => bleManager.CurrentAccess,
                    () => bleManager.RequestAccess().ToTask()
                ),
                new PermissionViewModel(
                    "BLE Host - Advertising",
                    () => bleHostingManager.AdvertisingAccessStatus,
                    () => bleHostingManager.RequestAccess(true, false)
                ),
                new PermissionViewModel(
                    "BLE Host - GATT",
                    () => bleHostingManager.GattAccessStatus,
                    () => bleHostingManager.RequestAccess(false, true)
                ),
                new PermissionViewModel(
                    "GPS - In App",
                    () => gpsManager.GetCurrentStatus(GpsRequest.Foreground),
                    () => gpsManager.RequestAccess(GpsRequest.Foreground)
                ),
                new PermissionViewModel(
                    "GPS - Background",
                    () => gpsManager.GetCurrentStatus(GpsRequest.Realtime(false)),
                    () => gpsManager.RequestAccess(GpsRequest.Realtime(false))
                ),
                new PermissionViewModel(
                    "Geofencing",
                    () => geofenceManager.CurrentStatus,
                    () => geofenceManager.RequestAccess()
                )
            };
            this.IsRefreshing = false;
        });
    }


    public ICommand Refresh { get; }
    [Reactive] public bool IsRefreshing { get; private set; }
    [Reactive] public List<PermissionViewModel> PermissionSet { get; private set; } 

    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Refresh.Execute(null);
    }
}


public class PermissionViewModel : ReactiveObject
{
    public PermissionViewModel(
        string description,
        Func<AccessState> getCurrent,
        Func<Task<AccessState>> requestAccess
    )
    {
        this.Description = description;
        this.Status = getCurrent.Invoke();
        this.Request = ReactiveCommand.CreateFromTask(async () =>
        {
            await requestAccess.Invoke();
            this.Status = getCurrent();
        });
    }


    public ICommand Request { get; }
    public string Description { get; }
    [Reactive] public AccessState Status { get; private set; }
}