using Shiny.BluetoothLE.Hosting;

namespace Sample.Linux.Pages.BLE;

[ShellMap<BleHostingPage>("blehosting")]
public partial class BleHostingViewModel(IBleHostingManager hostingManager) : ObservableObject
{
    const string ServiceUuid = "A495FF10-C5B1-4B44-B512-1370F02D74DE";
    const string CharacteristicUuid = "A495FF11-C5B1-4B44-B512-1370F02D74DE";

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string accessState = "Unknown";
    [ObservableProperty] bool isAdvertising;
    [ObservableProperty] string lastWrite = string.Empty;
    [ObservableProperty] int subscriberCount;

    IGattCharacteristic? hostedCharacteristic;


    [RelayCommand]
    async Task RequestAccess()
    {
        var access = await hostingManager.RequestAccess();
        this.AccessState = access.ToString();
    }


    [RelayCommand]
    async Task AddService()
    {
        try
        {
            await hostingManager.AddService(ServiceUuid, true, sb =>
            {
                this.hostedCharacteristic = sb.AddCharacteristic(CharacteristicUuid, cb =>
                {
                    cb.SetRead(_ => Task.FromResult(GattResult.Success(System.Text.Encoding.UTF8.GetBytes("hello from shiny"))));
                    cb.SetWrite(req =>
                    {
                        this.LastWrite = System.Text.Encoding.UTF8.GetString(req.Data);
                        return Task.CompletedTask;
                    });
                    cb.SetNotification(sub =>
                    {
                        this.SubscriberCount = this.hostedCharacteristic?.SubscribedCentrals.Count ?? 0;
                        return Task.CompletedTask;
                    });
                });
            });
            this.Status = "Service added";
        }
        catch (Exception ex)
        {
            this.Status = $"AddService error: {ex.Message}";
        }
    }


    [RelayCommand]
    void ClearServices()
    {
        hostingManager.ClearServices();
        this.hostedCharacteristic = null;
        this.SubscriberCount = 0;
        this.Status = "Services cleared";
    }


    [RelayCommand]
    async Task ToggleAdvertising()
    {
        try
        {
            if (hostingManager.IsAdvertising)
            {
                hostingManager.StopAdvertising();
                this.IsAdvertising = false;
                this.Status = "Advertising stopped";
            }
            else
            {
                await hostingManager.StartAdvertising(new AdvertisementOptions("ShinyLinux", ServiceUuid));
                this.IsAdvertising = true;
                this.Status = "Advertising started";
            }
        }
        catch (Exception ex)
        {
            this.Status = $"Advertising error: {ex.Message}";
        }
    }


    [RelayCommand]
    async Task NotifySubscribers()
    {
        if (this.hostedCharacteristic == null)
        {
            this.Status = "No characteristic - add the service first";
            return;
        }
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"tick {DateTime.Now:HH:mm:ss}");
            await this.hostedCharacteristic.Notify(bytes);
            this.Status = $"Notified {this.hostedCharacteristic.SubscribedCentrals.Count} central(s)";
        }
        catch (Exception ex)
        {
            this.Status = $"Notify error: {ex.Message}";
        }
    }
}
