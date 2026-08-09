using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Discovery;


[ShellMap<SsdpPage>("ssdp")]
public partial class SsdpViewModel(
    ISsdpManager ssdp,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    CancellationTokenSource? cancelSource;
    readonly Dictionary<string, SsdpDevice> lookup = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrowseText))]
    bool isBrowsing;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string searchTarget = SsdpConstants.SearchAll;

    List<SsdpItem> devices = [];
    public List<SsdpItem> Devices
    {
        get => this.devices;
        private set
        {
            this.devices = value;
            this.OnPropertyChanged();
        }
    }

    public string BrowseText => this.IsBrowsing ? "Stop" : "Browse";


    [RelayCommand]
    async Task ToggleBrowse()
    {
        if (this.IsBrowsing)
        {
            this.Stop();
            return;
        }

        this.lookup.Clear();
        this.Devices = [];
        this.IsBrowsing = true;
        this.Status = $"Searching {this.SearchTarget}";

        this.cancelSource = new CancellationTokenSource();
        var ct = this.cancelSource.Token;

        try
        {
            await foreach (var result in ssdp.Browse(this.SearchTarget, ct))
            {
                if (result.Status == SsdpBrowseStatus.Found)
                    this.lookup[result.Device.Udn] = result.Device;
                else
                    this.lookup.Remove(result.Device.Udn);

                this.Refresh();
            }
        }
        catch (OperationCanceledException)
        {
            // stopped by the user
        }
        catch (DiscoveryPermissionException ex)
        {
            // the message names the exact entitlement/permission that is missing
            await dialogs.Alert("Permission Required", ex.Message, "OK");
        }
        catch (DiscoveryException ex)
        {
            await dialogs.Alert("Discovery Error", ex.Message, "OK");
        }
        finally
        {
            this.Stop();
        }
    }


    /// <summary>
    /// Fetches the UPnP description for a tapped device, which is where the friendly name and
    /// model information live - an SSDP advertisement itself carries almost nothing readable.
    /// </summary>
    [RelayCommand]
    async Task Describe(SsdpItem? item)
    {
        if (item == null || !this.lookup.TryGetValue(item.Udn, out var device))
            return;

        try
        {
            var description = await ssdp.GetDescription(device);
            var services = String.Join("\n", description.Services.Select(x => x.ServiceType));

            await dialogs.Alert(
                description.FriendlyName ?? device.Udn,
                $"{description.Manufacturer} {description.ModelName}\n\n{services}",
                "OK"
            );
        }
        catch (SsdpException ex)
        {
            await dialogs.Alert("Description Failed", ex.Message, "OK");
        }
    }


    void Refresh()
    {
        var items = this.lookup.Values
            .OrderBy(x => x.Udn, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SsdpItem(
                x.Udn,
                x.Server ?? "(no server header)",
                x.Location?.AbsoluteUri ?? "(no location)",
                x.DeviceType ?? (x.IsRootDevice ? "root device" : "—"),
                $"{x.NotificationTypes.Count} advertisement(s), {x.ServiceTypes.Count} service(s)"
            ))
            .ToList();

        mainThread.BeginInvokeOnMainThread(() =>
        {
            this.Devices = items;
            this.Status = $"{items.Count} device(s)";
        });
    }


    void Stop()
    {
        this.cancelSource?.Cancel();
        this.cancelSource?.Dispose();
        this.cancelSource = null;
        this.IsBrowsing = false;
    }


    public void Dispose() => this.Stop();
}


public record SsdpItem(string Udn, string Server, string Location, string DeviceType, string Summary);