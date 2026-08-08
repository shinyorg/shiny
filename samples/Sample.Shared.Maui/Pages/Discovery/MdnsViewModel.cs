using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Discovery;


[ShellMap<MdnsPage>("mdns")]
public partial class MdnsViewModel(
    IMdnsManager mdns,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    CancellationTokenSource? cancelSource;
    readonly Dictionary<string, MdnsService> lookup = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrowseText))]
    bool isBrowsing;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string serviceType = "_http._tcp";

    // a plain list reassigned in bulk - the collection is small and always replaced wholesale
    List<MdnsItem> services = [];
    public List<MdnsItem> Services
    {
        get => this.services;
        private set
        {
            this.services = value;
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
        this.Services = [];
        this.IsBrowsing = true;
        this.Status = $"Browsing {this.ServiceType}";

        this.cancelSource = new CancellationTokenSource();
        var ct = this.cancelSource.Token;

        try
        {
            await foreach (var result in mdns.Browse(this.ServiceType, ct))
            {
                if (result.Status == MdnsBrowseStatus.Found)
                    this.lookup[result.Service.FullName] = result.Service;
                else
                    this.lookup.Remove(result.Service.FullName);

                this.Refresh();
            }
        }
        catch (OperationCanceledException)
        {
            // stopped by the user
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


    void Refresh()
    {
        var items = this.lookup.Values
            .OrderBy(x => x.InstanceName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new MdnsItem(
                x.InstanceName,
                x.IsResolved ? $"{x.HostName}:{x.Port}" : "(unresolved)",
                String.Join(", ", x.Addresses.Select(a => a.ToString())),
                String.Join("  ", x.TxtRecords.Select(t => $"{t.Key}={t.Value}"))
            ))
            .ToList();

        mainThread.BeginInvokeOnMainThread(() =>
        {
            this.Services = items;
            this.Status = $"{items.Count} service(s)";
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


public record MdnsItem(string Name, string Endpoint, string Addresses, string Txt);