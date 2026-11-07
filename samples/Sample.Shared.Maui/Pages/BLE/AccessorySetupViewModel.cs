using System.Text;
using Sample.Shared.Maui.Services;
using Shiny.BluetoothLE.AccessorySetup;

namespace Sample.Shared.Maui.Pages.BLE;

public record AccessoryItem(SetupAccessory Accessory, bool IsObservingPresence)
{
    public string Title => this.Accessory.DisplayName ?? this.Accessory.Identifier;

    public string Detail =>
        $"{this.Accessory.State} · id {this.Accessory.Identifier}" +
        (this.Accessory.MacAddress == null ? "" : $" · {this.Accessory.MacAddress}") +
        (this.IsObservingPresence ? " · observing presence" : "");

    public string PresenceText => this.IsObservingPresence ? "Stop Presence" : "Presence";
}


[ShellMap<AccessorySetupPage>("accessorysetup")]
public partial class AccessorySetupViewModel(
    IBleManager bleManager,
    IEventStore events,
    IAccessorySetupManager? setup = null   // only registered on iOS & Android
) : ObservableObject, IPageLifecycleAware
{
    readonly HashSet<string> observing = [];
    EventHandler<EventRecord>? eventHandler;

    [ObservableProperty] string support = "";
    [ObservableProperty] string status = "Idle";
    [ObservableProperty] bool isSupported;
    [ObservableProperty] bool isPresenceSupported;
    [ObservableProperty] bool useNameFilter;
    [ObservableProperty] bool immediateRange;
    [ObservableProperty] bool requiresPairing;
    [ObservableProperty] bool singleDevice;

    public List<AccessoryItem> Accessories
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];

    public List<string> Log
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];


    public void OnAppearing()
    {
        this.IsSupported = setup?.IsSupported ?? false;
        this.IsPresenceSupported = setup?.IsPresenceSupported ?? false;
        this.Support = setup == null
            ? "Not available on this platform (iOS 18+ and Android only)"
            : $"Supported: {this.IsSupported} · Presence: {this.IsPresenceSupported}";

        this.eventHandler = (_, record) =>
        {
            if (record.Category == SampleAccessory.EventCategory)
                MainThread.BeginInvokeOnMainThread(() => this.Log = [Format(record), .. this.Log]);
        };
        events.EventAdded += this.eventHandler;

        _ = this.LoadLog();
        _ = this.Refresh();
    }


    public void OnDisappearing()
    {
        if (this.eventHandler != null)
            events.EventAdded -= this.eventHandler;
        this.eventHandler = null;
    }


    [RelayCommand]
    async Task ShowPicker()
    {
        if (setup == null || !setup.IsSupported)
        {
            await this.Write("Picker unavailable - accessory setup is not supported here");
            return;
        }

        var filter = new AccessoryDiscoveryFilter
        {
            ServiceUuid = SampleAccessory.ServiceUuid,
            NameSubstring = this.UseNameFilter ? SampleAccessory.LocalName : null,
            Range = this.ImmediateRange ? AccessoryRange.Immediate : AccessoryRange.Default,
            RequiresPairing = this.RequiresPairing
        };
        await this.Write(
            $"Picker opened - service {filter.ServiceUuid}" +
            (filter.NameSubstring == null ? "" : $", name contains '{filter.NameSubstring}'") +
            $", range {filter.Range}" +
            (filter.RequiresPairing ? ", pairing" : "") +
            (this.SingleDevice ? ", single device (Android)" : "")
        );

        try
        {
            this.Status = "Waiting for the picker...";
            var accessory = await setup.ShowPicker(
                new AccessoryPickerItem("Shiny Sample Accessory", filter)
                {
                    ProductImage = "shiny.png",
                    AllowRename = true
                },
                new AccessoryPickerOptions
                {
                    AndroidSingleDevice = this.SingleDevice,
                    DiscoveryTimeout = TimeSpan.FromSeconds(60)
                }
            );

            if (accessory == null)
            {
                this.Status = "Picker closed";
                await this.Write("Picker closed - dismissed, or nothing matched the filter");
            }
            else
            {
                this.Status = $"Picked {accessory.DisplayName ?? accessory.Identifier}";
                await this.Write($"Picked '{accessory.DisplayName ?? accessory.Identifier}' ({accessory.State}, peripheral {accessory.PeripheralUuid ?? "none"})", accessory);
            }
        }
        catch (AccessorySetupException ex)
        {
            this.Status = $"Picker failed: {ex.Code}";
            await this.Write($"Picker failed ({ex.Code}): {ex.Message}");
        }
        catch (Exception ex)
        {
            this.Status = "Picker failed";
            await this.Write($"Picker failed: {ex.Message}");
        }
        await this.Refresh();
    }


    [RelayCommand]
    async Task Refresh()
    {
        if (setup == null)
            return;

        try
        {
            var list = await setup.GetAccessories();
            this.Accessories = list
                .Select(x => new AccessoryItem(x, this.observing.Contains(x.Identifier)))
                .ToList();
        }
        catch (Exception ex)
        {
            await this.Write($"Listing accessories failed: {ex.Message}");
        }
    }


    [RelayCommand]
    async Task ConnectAndRead(AccessoryItem item)
    {
        var name = item.Title;
        var access = await bleManager.RequestAccessAsync();
        if (access != AccessState.Available)
        {
            await this.Write($"Connect to '{name}' skipped - Bluetooth access is {access}");
            return;
        }

        var peripheral = bleManager.GetPeripheral(item.Accessory);
        if (peripheral == null)
        {
            await this.Write($"'{name}' has no peripheral - Bluetooth does not know it; pick it again");
            return;
        }

        try
        {
            this.Status = $"Connecting to {name}...";
            await peripheral.ConnectAsync(timeout: TimeSpan.FromSeconds(20));
            await this.Write($"Connected to '{name}' ({peripheral.Uuid})");

            var result = await peripheral.ReadCharacteristicAsync(SampleAccessory.ServiceUuid, SampleAccessory.InfoCharacteristicUuid);
            var text = result.Data == null ? "(empty)" : Encoding.UTF8.GetString(result.Data);
            await this.Write($"Read from '{name}': {text}");

            await peripheral.WriteCharacteristicAsync(
                SampleAccessory.ServiceUuid,
                SampleAccessory.PingCharacteristicUuid,
                Encoding.UTF8.GetBytes($"Hello from {Microsoft.Maui.Devices.DeviceInfo.Current.Name}")
            );
            await this.Write($"Pinged '{name}'");
            this.Status = $"Talked to {name}";
        }
        catch (Exception ex)
        {
            this.Status = $"Connection to {name} failed";
            await this.Write($"Connect/read '{name}' failed: {ex.Message}");
        }
        finally
        {
            peripheral.CancelConnection();
        }
    }


    [RelayCommand]
    async Task TogglePresence(AccessoryItem item)
    {
        if (setup == null || !setup.IsPresenceSupported)
        {
            await this.Write("Presence needs Android 12 or later");
            return;
        }

        try
        {
            if (item.IsObservingPresence)
            {
                await setup.StopObservingPresence(item.Accessory);
                this.observing.Remove(item.Accessory.Identifier);
                await this.Write($"Stopped observing presence for '{item.Title}'", item.Accessory);
            }
            else
            {
                await setup.StartObservingPresence(item.Accessory);
                this.observing.Add(item.Accessory.Identifier);
                await this.Write($"Observing presence for '{item.Title}' - turn the advertiser off and on to see events", item.Accessory);
            }
        }
        catch (AccessorySetupException ex)
        {
            await this.Write($"Presence for '{item.Title}' failed ({ex.Code}): {ex.Message}");
        }
        await this.Refresh();
    }


    [RelayCommand]
    async Task Remove(AccessoryItem item)
    {
        if (setup == null)
            return;

        try
        {
            await setup.Remove(item.Accessory);
            this.observing.Remove(item.Accessory.Identifier);
            await this.Write($"Removed '{item.Title}'", item.Accessory);
        }
        catch (Exception ex)
        {
            await this.Write($"Removing '{item.Title}' failed: {ex.Message}");
        }
        await this.Refresh();
    }


    [RelayCommand]
    void ClearLog() => this.Log = [];


    async Task LoadLog()
    {
        var records = await events.GetAll(SampleAccessory.EventCategory, limit: 100);
        this.Log = records.Select(Format).ToList();
    }


    // goes through the event store so it persists, lands on the Events page and comes back via EventAdded
    Task Write(string text, SetupAccessory? accessory = null)
        => events.Add(SampleAccessory.EventCategory, text, accessory == null ? null : SampleAccessorySetupDelegate.Metadata(accessory));


    static string Format(EventRecord record) => $"{record.Timestamp.ToLocalTime():T}  {record.Description}";
}
