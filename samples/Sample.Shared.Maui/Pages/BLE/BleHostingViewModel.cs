using System.Text;
using Microsoft.Maui.Dispatching;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.Pages.BLE;

[ShellMap<BleHostingPage>("blehosting")]
public partial class BleHostingViewModel(IBleHostingManager hostingManager) : ObservableObject, IDisposable
{
    const string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
    const string ReadCharUuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";
    const string WriteCharUuid = "A495FF22-C5B1-4B44-B512-1370F02D74DE";
    const string NotifyCharUuid = "A495FF23-C5B1-4B44-B512-1370F02D74DE";
    static readonly Guid BeaconUuid = Guid.Parse("A495FF30-C5B1-4B44-B512-1370F02D74DE");

    IGattService? service;
    IGattCharacteristic? notifyCharacteristic;
    IDispatcherTimer? autoNotifyTimer;
    int readCount;
    int notifyCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdvertiseText))]
    bool isAdvertising;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BeaconText))]
    bool isBeaconing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoNotifyText))]
    bool autoNotify;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string advertisingAccess = "Unknown";
    [ObservableProperty] string gattAccess = "Unknown";
    [ObservableProperty] string localName = "ShinySample";
    [ObservableProperty] int subscriberCount;
    [ObservableProperty] int readRequestCount;
    [ObservableProperty] int notificationsSent;
    [ObservableProperty] string lastWriteValue = "(none)";
    [ObservableProperty] string notifyPayload = "Ping";
    [ObservableProperty] ushort beaconMajor = 1;
    [ObservableProperty] ushort beaconMinor = 100;

    public string AdvertiseText => this.IsAdvertising ? "Stop GATT Advertising" : "Start GATT Advertising";
    public string BeaconText => this.IsBeaconing ? "Stop iBeacon" : "Start iBeacon";
    public string AutoNotifyText => this.AutoNotify ? "Stop Auto Notify" : "Start Auto Notify (1s)";

    [RelayCommand]
    async Task CheckAccess()
    {
        var access = await hostingManager.RequestAccess();
        this.AdvertisingAccess = hostingManager.AdvertisingAccessStatus.ToString();
        this.GattAccess = hostingManager.GattAccessStatus.ToString();
        this.Status = $"Access: {access}";
    }

    [RelayCommand]
    async Task ToggleAdvertising()
    {
        if (this.IsAdvertising)
        {
            this.StopAutoNotifyTimer();
            hostingManager.StopAdvertising();
            if (this.service != null)
            {
                hostingManager.RemoveService(ServiceUuid);
                this.service = null;
                this.notifyCharacteristic = null;
            }
            this.IsAdvertising = false;
            this.SubscriberCount = 0;
            this.Status = "Stopped";
            return;
        }

        var access = await hostingManager.RequestAccess();
        this.AdvertisingAccess = hostingManager.AdvertisingAccessStatus.ToString();
        this.GattAccess = hostingManager.GattAccessStatus.ToString();
        if (access != AccessState.Available)
        {
            this.Status = $"Access denied: {access}";
            return;
        }

        this.service = await hostingManager.AddService(ServiceUuid, true, sb =>
        {
            sb.AddCharacteristic(ReadCharUuid, cb =>
            {
                cb.SetRead(_ =>
                {
                    var count = Interlocked.Increment(ref this.readCount);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        this.ReadRequestCount = count;
                        this.Status = $"Read #{count}";
                    });
                    var payload = $"Hello #{count} @ {DateTime.Now:T}";
                    return Task.FromResult(GattResult.Success(Encoding.UTF8.GetBytes(payload)));
                });
            });

            sb.AddCharacteristic(WriteCharUuid, cb =>
            {
                cb.SetWrite(request =>
                {
                    var value = Encoding.UTF8.GetString(request.Data);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        this.LastWriteValue = value;
                        this.Status = $"Write received: {value}";
                    });
                    if (request.IsReplyNeeded)
                        request.Respond(GattState.Success);
                    return Task.CompletedTask;
                }, WriteOptions.Write);
            });

            this.notifyCharacteristic = sb.AddCharacteristic(NotifyCharUuid, cb =>
            {
                cb.SetNotification(sub =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        this.SubscriberCount = sub.Characteristic.SubscribedCentrals.Count;
                        this.Status = sub.IsSubscribing
                            ? $"Central subscribed ({this.SubscriberCount})"
                            : $"Central unsubscribed ({this.SubscriberCount})";
                    });
                    return Task.CompletedTask;
                }, NotificationOptions.Notify);
            });
        });

        await hostingManager.StartAdvertising(new AdvertisementOptions(this.LocalName, ServiceUuid));
        this.IsAdvertising = true;
        this.Status = $"Advertising as '{this.LocalName}'";
    }

    [RelayCommand]
    async Task SendNotification()
    {
        if (this.notifyCharacteristic == null)
        {
            this.Status = "Not advertising";
            return;
        }
        if (this.SubscriberCount == 0)
        {
            this.Status = "No subscribers";
            return;
        }

        var count = Interlocked.Increment(ref this.notifyCount);
        var data = Encoding.UTF8.GetBytes($"{this.NotifyPayload} #{count}");
        await this.notifyCharacteristic.Notify(data);
        this.NotificationsSent = count;
        this.Status = $"Sent notification #{count}";
    }

    [RelayCommand]
    void ToggleAutoNotify()
    {
        if (this.AutoNotify)
        {
            this.StopAutoNotifyTimer();
            return;
        }
        if (this.notifyCharacteristic == null)
        {
            this.Status = "Start advertising first";
            return;
        }

        this.autoNotifyTimer = Application.Current?.Dispatcher.CreateTimer();
        if (this.autoNotifyTimer == null)
            return;

        this.autoNotifyTimer.Interval = TimeSpan.FromSeconds(1);
        this.autoNotifyTimer.Tick += async (_, _) => await this.SendNotification();
        this.autoNotifyTimer.Start();
        this.AutoNotify = true;
    }

    [RelayCommand]
    async Task ToggleBeacon()
    {
        if (this.IsBeaconing)
        {
            hostingManager.StopAdvertising();
            this.IsBeaconing = false;
            this.Status = "Beacon stopped";
            return;
        }

        var access = await hostingManager.RequestAccess(advertise: true, connect: false);
        if (access != AccessState.Available)
        {
            this.Status = $"Access denied: {access}";
            return;
        }

        await hostingManager.AdvertiseBeacon(BeaconUuid, this.BeaconMajor, this.BeaconMinor);
        this.IsBeaconing = true;
        this.Status = $"Beaconing {BeaconUuid} {this.BeaconMajor}/{this.BeaconMinor}";
    }

    void StopAutoNotifyTimer()
    {
        this.autoNotifyTimer?.Stop();
        this.autoNotifyTimer = null;
        this.AutoNotify = false;
    }

    public void Dispose()
    {
        this.StopAutoNotifyTimer();
        if (this.IsAdvertising || this.IsBeaconing)
        {
            hostingManager.StopAdvertising();
            hostingManager.ClearServices();
        }
    }
}
