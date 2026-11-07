using System.Text;
using Sample.Shared.Maui.Services;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.Pages.BLE;

/// <summary>
/// Turns this device into the accessory the Accessory Setup page looks for: it advertises
/// <see cref="SampleAccessory.ServiceUuid"/> under the name "Shiny" and hosts a small GATT service - an info
/// characteristic to read and a ping characteristic to write.
/// </summary>
[ShellMap<AccessoryAdvertiserPage>("accessoryadvertiser")]
public partial class AccessoryAdvertiserViewModel(
    IBleHostingManager hostingManager,
    IEventStore events
) : ObservableObject, IDisposable
{
    int readCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdvertiseText))]
    [NotifyPropertyChangedFor(nameof(CanEditName))]
    bool isAdvertising;

    // the advertised local name - the Accessory Setup page's name filter only matches names containing "Shiny"
    [ObservableProperty] string localName = SampleAccessory.LocalName;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] int readsServed;
    [ObservableProperty] string lastPing = "(none)";

    public string AdvertiseText => this.IsAdvertising ? "Stop Advertising" : "Start Advertising";
    public string ServiceUuid => SampleAccessory.ServiceUuid;
    public bool CanEditName => !this.IsAdvertising;

    public List<string> Log
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];


    [RelayCommand]
    async Task ToggleAdvertising()
    {
        if (this.IsAdvertising)
        {
            this.Stop();
            await this.Write("Advertiser stopped");
            return;
        }

        var name = this.LocalName?.Trim();
        if (String.IsNullOrEmpty(name))
        {
            this.Status = "Enter a device name";
            return;
        }

        var access = await hostingManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access denied: {access}";
            await this.Write($"Advertiser could not start - hosting access is {access}");
            return;
        }

        try
        {
            await hostingManager.AddService(SampleAccessory.ServiceUuid, true, sb =>
            {
                sb.AddCharacteristic(SampleAccessory.InfoCharacteristicUuid, cb => cb.SetRead(request =>
                {
                    var count = Interlocked.Increment(ref this.readCount);
                    MainThread.BeginInvokeOnMainThread(() => this.ReadsServed = count);
                    _ = this.Write($"Info read #{count} by central {request.Peripheral.Uuid}");

                    var payload = $"{name} on {Microsoft.Maui.Devices.DeviceInfo.Current.Name} ({Microsoft.Maui.Devices.DeviceInfo.Current.Platform}) read #{count} @ {DateTime.Now:T}";
                    return Task.FromResult(GattResult.Success(Encoding.UTF8.GetBytes(payload)));
                }));

                sb.AddCharacteristic(SampleAccessory.PingCharacteristicUuid, cb => cb.SetWrite(request =>
                {
                    var text = Encoding.UTF8.GetString(request.Data);
                    MainThread.BeginInvokeOnMainThread(() => this.LastPing = text);
                    _ = this.Write($"Ping from central {request.Peripheral.Uuid}: {text}");

                    if (request.IsReplyNeeded)
                        request.Respond(GattState.Success);
                    return Task.CompletedTask;
                }, WriteOptions.Write));
            });

            await hostingManager.StartAdvertising(new AdvertisementOptions(name, SampleAccessory.ServiceUuid));
            this.IsAdvertising = true;
            this.Status = $"Advertising as '{name}'";
            await this.Write($"Advertiser started - '{name}', service {SampleAccessory.ServiceUuid}");
        }
        catch (Exception ex)
        {
            this.Stop();
            this.Status = "Advertising failed";
            await this.Write($"Advertiser failed: {ex.Message}");
        }
    }


    [RelayCommand]
    void ClearLog() => this.Log = [];


    void Stop()
    {
        hostingManager.StopAdvertising();
        hostingManager.RemoveService(SampleAccessory.ServiceUuid);
        this.IsAdvertising = false;
        this.Status = "Stopped";
    }


    // shown here and recorded in the event store, so the Events page has both sides of a test
    Task Write(string text)
    {
        MainThread.BeginInvokeOnMainThread(() => this.Log = [$"{DateTime.Now:T}  {text}", .. this.Log]);
        return events.Add(SampleAccessory.EventCategory, "Advertiser: " + text);
    }


    public void Dispose()
    {
        if (this.IsAdvertising)
            this.Stop();
    }
}
