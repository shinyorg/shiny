using Shiny.Infrastructure;
using Shiny.Net.Wifi;

namespace Sample.Shared.Maui.Pages.Wifi;


[ShellMap<WifiPage>("wifi")]
public partial class WifiViewModel(
    IWifiManager wifi,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    bool hooked;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string capabilities = String.Empty;
    [ObservableProperty] string currentNetwork = "(unknown)";
    [ObservableProperty] string radioState = "(unknown)";
    [ObservableProperty] string ssid = String.Empty;
    [ObservableProperty] string passphrase = String.Empty;
    [ObservableProperty] bool isBusy;

    // plain lists reassigned in bulk - both are small and always replaced wholesale
    List<WifiItem> networks = [];
    public List<WifiItem> Networks
    {
        get => this.networks;
        private set
        {
            this.networks = value;
            this.OnPropertyChanged();
        }
    }

    List<KnownItem> knownNetworks = [];
    public List<KnownItem> KnownNetworks
    {
        get => this.knownNetworks;
        private set
        {
            this.knownNetworks = value;
            this.OnPropertyChanged();
        }
    }

    // the page only offers what this platform actually supports - see WifiCapabilities
    public bool CanScan => wifi.Capabilities.HasFlag(WifiCapabilities.Scan);
    public bool CanConnect => wifi.Capabilities.HasFlag(WifiCapabilities.Connect);
    public bool CanDisconnect => wifi.Capabilities.HasFlag(WifiCapabilities.Disconnect);
    public bool CanListKnown => wifi.Capabilities.HasFlag(WifiCapabilities.KnownNetworks);
    public bool CanForget => wifi.Capabilities.HasFlag(WifiCapabilities.ForgetNetwork);
    public bool CanConnectKnown => wifi.Capabilities.HasFlag(WifiCapabilities.ConnectKnownNetwork);
    public bool CanToggleRadio => wifi.Capabilities.HasFlag(WifiCapabilities.RadioToggle);


    [RelayCommand]
    async Task Load()
    {
        this.Capabilities = wifi.Capabilities == WifiCapabilities.None
            ? "None - no Wi-Fi API on this platform"
            : wifi.Capabilities.ToString();

        if (!this.hooked)
        {
            wifi.Changed += this.OnChanged;
            this.hooked = true;
        }

        this.RefreshCurrent();
        await this.ReadRadio();
    }


    [RelayCommand]
    async Task RequestAccess()
    {
        var result = await this.Run(async () =>
        {
            var access = await wifi.RequestAccess();
            this.Status = $"Access: {access}";
            return access;
        });

        if (result == AccessState.Available)
            this.RefreshCurrent();
    }


    [RelayCommand]
    async Task Scan()
    {
        await this.Run(async () =>
        {
            this.Status = "Scanning...";
            var results = await wifi.Scan();

            this.Networks = results
                .Select(x => new WifiItem(
                    x.Ssid.Length == 0 ? "(hidden)" : x.Ssid,
                    $"{x.Security} - {x.Band} ch {x.Channel?.ToString() ?? "?"}",
                    $"{x.SignalStrengthPercent}%  {x.SignalStrengthDbm?.ToString() ?? "?"} dBm  {x.Bssid ?? ""}"
                ))
                .ToList();

            this.Status = $"{this.Networks.Count} network(s)";
            return true;
        });
    }


    [RelayCommand]
    async Task Connect()
    {
        if (String.IsNullOrWhiteSpace(this.Ssid))
        {
            await dialogs.Alert("Wi-Fi", "Enter an SSID first", "OK");
            return;
        }

        await this.Run(async () =>
        {
            this.Status = $"Joining {this.Ssid}...";
            var request = new WifiConnectionRequest(this.Ssid)
            {
                Passphrase = String.IsNullOrWhiteSpace(this.Passphrase) ? null : this.Passphrase
            };
            var info = await wifi.Connect(request);
            this.Status = $"Joined {info.Ssid ?? this.Ssid}";
            this.RefreshCurrent();
            return true;
        });
    }


    [RelayCommand]
    async Task ConnectKnown(KnownItem item)
        => await this.Run(async () =>
        {
            this.Status = $"Rejoining {item.Ssid}...";
            var info = await wifi.Connect(item.Id);
            this.Status = $"Joined {info.Ssid ?? item.Ssid}";
            this.RefreshCurrent();
            return true;
        });


    [RelayCommand]
    async Task Disconnect()
        => await this.Run(async () =>
        {
            await wifi.Disconnect();
            this.Status = "Disconnected";
            this.RefreshCurrent();
            return true;
        });


    [RelayCommand]
    async Task LoadKnown()
        => await this.Run(async () =>
        {
            var known = await wifi.GetKnownNetworks();
            this.KnownNetworks = known
                .Select(x => new KnownItem(
                    x.Id,
                    x.Ssid,
                    $"{x.Security}{(x.IsHidden ? " - hidden" : "")}{(x.AddedByThisApp ? " - added by this app" : "")}"
                ))
                .ToList();

            this.Status = $"{this.KnownNetworks.Count} saved network(s)";
            return true;
        });


    [RelayCommand]
    async Task Forget(KnownItem item)
        => await this.Run(async () =>
        {
            await wifi.Forget(item.Id);
            this.Status = $"Forgot {item.Ssid}";
            await this.LoadKnown();
            return true;
        });


    [RelayCommand]
    async Task ToggleRadio()
        => await this.Run(async () =>
        {
            var on = await wifi.GetRadioEnabled();
            await wifi.SetRadioEnabled(!on);
            this.Status = $"Radio {(on ? "off" : "on")}";
            await this.ReadRadio();
            return true;
        });


    async Task ReadRadio()
    {
        try
        {
            this.RadioState = await wifi.GetRadioEnabled() ? "On" : "Off";
        }
        catch (WifiException ex)
        {
            this.RadioState = ex is WifiNotSupportedException ? "(not supported)" : "(unavailable)";
        }
    }


    void RefreshCurrent()
    {
        var info = wifi.CurrentNetwork;
        this.CurrentNetwork = info == null
            ? "(not connected)"
            : $"{info.Ssid ?? "(ssid hidden)"}\n{info.InterfaceName}  {info.Security}\nIP {info.IPv4Address?.ToString() ?? "-"}  GW {info.Gateway?.ToString() ?? "-"}";
    }


    void OnChanged(object? sender, WifiNetworkInfo? info)
        => mainThread.BeginInvokeOnMainThread(() =>
        {
            this.RefreshCurrent();
            this.Status = info == null ? "Left the network" : $"Now on {info.Ssid ?? "(ssid hidden)"}";
        });


    // every call funnels through here so the capability exceptions surface as dialogs
    // rather than crashing the page
    async Task<T?> Run<T>(Func<Task<T>> work)
    {
        if (this.IsBusy)
            return default;

        this.IsBusy = true;
        try
        {
            return await work();
        }
        catch (WifiNotSupportedException ex)
        {
            this.Status = "Not supported";
            await dialogs.Alert("Not Supported", ex.Message, "OK");
        }
        catch (WifiPermissionException ex)
        {
            this.Status = "Permission denied";
            await dialogs.Alert("Permission", ex.Message, "OK");
        }
        catch (WifiException ex)
        {
            this.Status = "Failed";
            await dialogs.Alert("Wi-Fi Error", ex.Message, "OK");
        }
        finally
        {
            this.IsBusy = false;
        }
        return default;
    }


    public void Dispose()
    {
        if (this.hooked)
        {
            wifi.Changed -= this.OnChanged;
            this.hooked = false;
        }
    }
}


public record WifiItem(string Ssid, string Detail, string Signal);
public record KnownItem(string Id, string Ssid, string Detail);
