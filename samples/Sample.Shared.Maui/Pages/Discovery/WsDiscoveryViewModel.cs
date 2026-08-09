using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Discovery;


[ShellMap<WsDiscoveryPage>("wsdiscovery")]
public partial class WsDiscoveryViewModel(
    IWsDiscoveryManager wsd,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    CancellationTokenSource? cancelSource;
    readonly Dictionary<string, WsdTarget> lookup = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrowseText))]
    bool isBrowsing;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] bool onvifOnly;

    List<WsdItem> targets = [];
    public List<WsdItem> Targets
    {
        get => this.targets;
        private set
        {
            this.targets = value;
            this.OnPropertyChanged();
        }
    }

    public string BrowseText => this.IsBrowsing ? "Stop" : "Probe";


    [RelayCommand]
    async Task ToggleBrowse()
    {
        if (this.IsBrowsing)
        {
            this.Stop();
            return;
        }

        this.lookup.Clear();
        this.Targets = [];
        this.IsBrowsing = true;
        this.Status = this.OnvifOnly ? "Probing for ONVIF cameras" : "Probing for everything";

        this.cancelSource = new CancellationTokenSource();
        var ct = this.cancelSource.Token;

        try
        {
            // an untyped probe finds more hardware than a typed one - filter locally instead
            await foreach (var result in wsd.Browse(ct))
            {
                if (result.Status == WsDiscoveryStatus.Found)
                {
                    if (!this.OnvifOnly || result.Target.IsOnvifCamera)
                        this.lookup[result.Target.EndpointReference] = result.Target;
                }
                else
                {
                    this.lookup.Remove(result.Target.EndpointReference);
                }
                this.Refresh();
            }
        }
        catch (OperationCanceledException)
        {
            // stopped by the user
        }
        catch (DiscoveryPermissionException ex)
        {
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


    void Refresh()
    {
        var items = this.lookup.Values
            .OrderBy(x => x.EndpointReference, StringComparer.OrdinalIgnoreCase)
            .Select(x => new WsdItem(
                x.GetScopeValue("name") ?? x.EndpointReference,
                x.PreferredAddress?.AbsoluteUri ?? "(no address)",
                String.Join(", ", x.Types.Select(t => t.Name)),
                x.GetScopeValue("hardware") ?? "—",
                $"{x.Profile}  v{x.MetadataVersion}{(x.IsOnvifCamera ? "  ONVIF" : String.Empty)}"
            ))
            .ToList();

        mainThread.BeginInvokeOnMainThread(() =>
        {
            this.Targets = items;
            this.Status = $"{items.Count} target(s)";
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


public record WsdItem(string Name, string Address, string Types, string Hardware, string Detail);