using Shiny.BluetoothLE.Hosting;

namespace Sample.Maui.Pages.BLE;

[ShellMap<BleHostingPage>("blehosting")]
public partial class BleHostingViewModel(IBleHostingManager hostingManager) : ObservableObject, IDisposable
{
    static readonly string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
    static readonly string CharacteristicUuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";

    IGattService? service;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdvertiseText))]
    bool isAdvertising;

    [ObservableProperty] string status = "Not advertising";
    [ObservableProperty] int subscriberCount;

    public string AdvertiseText => this.IsAdvertising ? "Stop Advertising" : "Start Advertising";

    [RelayCommand]
    async Task ToggleAdvertising()
    {
        if (this.IsAdvertising)
        {
            hostingManager.StopAdvertising();
            if (this.service != null)
            {
                hostingManager.RemoveService(ServiceUuid);
                this.service = null;
            }
            this.IsAdvertising = false;
            this.Status = "Not advertising";
            return;
        }

        var access = await hostingManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        this.service = await hostingManager.AddService(ServiceUuid, true, sb =>
        {
            sb.AddCharacteristic(CharacteristicUuid, cb =>
            {
                cb.SetRead(request => Task.FromResult(GattResult.Success(System.Text.Encoding.UTF8.GetBytes("Hello from Shiny"))));
                cb.SetWrite(request =>
                {
                    var value = System.Text.Encoding.UTF8.GetString(request.Data);
                    MainThread.BeginInvokeOnMainThread(() => this.Status = $"Received: {value}");
                    return Task.CompletedTask;
                });
                cb.SetNotification(sub =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        this.SubscriberCount += sub.IsSubscribing ? 1 : -1;
                    });
                    return Task.CompletedTask;
                });
            });
        });

        await hostingManager.StartAdvertising(new AdvertisementOptions
        {
            LocalName = "ShinySample"
        });
        this.IsAdvertising = true;
        this.Status = "Advertising...";
    }

    [RelayCommand]
    async Task SendNotification()
    {
        if (this.service == null) return;
        var data = System.Text.Encoding.UTF8.GetBytes($"Notify: {DateTime.Now:T}");
        this.Status = "Notification sent";
    }

    public void Dispose()
    {
        if (this.IsAdvertising)
        {
            hostingManager.StopAdvertising();
            hostingManager.ClearServices();
        }
    }
}
