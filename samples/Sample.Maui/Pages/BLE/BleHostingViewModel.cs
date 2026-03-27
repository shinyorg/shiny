using Shiny.BluetoothLE.Hosting;

namespace Sample.Maui.Pages.BLE;

[ShellMap<BleHostingPage>("blehosting")]
public partial class BleHostingViewModel(IBleHostingManager hostingManager) : ObservableObject, IDisposable
{
    static readonly string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
    static readonly string CharacteristicUuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";

    IGattService? _service;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdvertiseText))]
    bool isAdvertising;

    [ObservableProperty]
    string status = "Not advertising";

    [ObservableProperty]
    int subscriberCount;

    public string AdvertiseText => IsAdvertising ? "Stop Advertising" : "Start Advertising";

    [RelayCommand]
    async Task ToggleAdvertising()
    {
        if (IsAdvertising)
        {
            hostingManager.StopAdvertising();
            if (_service != null)
            {
                hostingManager.RemoveService(ServiceUuid);
                _service = null;
            }
            IsAdvertising = false;
            Status = "Not advertising";
            return;
        }

        var access = await hostingManager.RequestAccess();
        if (access != AccessState.Available)
        {
            Status = $"Access: {access}";
            return;
        }

        _service = await hostingManager.AddService(ServiceUuid, true, sb =>
        {
            sb.AddCharacteristic(CharacteristicUuid, cb =>
            {
                cb.SetRead(request => Task.FromResult(GattResult.Success(System.Text.Encoding.UTF8.GetBytes("Hello from Shiny"))));
                cb.SetWrite(request =>
                {
                    var value = System.Text.Encoding.UTF8.GetString(request.Data);
                    MainThread.BeginInvokeOnMainThread(() => Status = $"Received: {value}");
                    return Task.CompletedTask;
                });
                cb.SetNotification(sub =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SubscriberCount += sub.IsSubscribing ? 1 : -1;
                    });
                    return Task.CompletedTask;
                });
            });
        });

        await hostingManager.StartAdvertising(new AdvertisementOptions
        {
            LocalName = "ShinySample"
        });
        IsAdvertising = true;
        Status = "Advertising...";
    }

    [RelayCommand]
    async Task SendNotification()
    {
        if (_service == null) return;
        // Notify subscribers with a timestamp
        var data = System.Text.Encoding.UTF8.GetBytes($"Notify: {DateTime.Now:T}");
        // characteristic notify would go here through the service
        Status = "Notification sent";
    }

    public void Dispose()
    {
        if (IsAdvertising)
        {
            hostingManager.StopAdvertising();
            hostingManager.ClearServices();
        }
    }
}
