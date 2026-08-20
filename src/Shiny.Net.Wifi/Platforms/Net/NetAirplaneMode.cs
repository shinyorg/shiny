namespace Shiny.Net.Wifi;


/// <summary>
/// Airplane mode stub for plain .NET. There is no such concept on a headless host, and no settings
/// UI to send anyone to.
/// </summary>
public class NetAirplaneMode : IAirplaneMode
{
    public bool IsSupported => false;
    public bool CanToggle => false;
    public bool IsEnabled => false;

    // never raised - nothing here can change
    public event EventHandler<bool>? Changed { add { } remove { } }

    public Task SetEnabled(bool enabled, CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.AirplaneModeToggle,
            "the base Shiny.Net.Wifi package has no native backend for plain .NET. On Linux, reference Shiny.Net.Wifi.Linux for the NetworkManager implementation"
        );

    public Task OpenSettings(CancellationToken ct = default)
        => throw new WifiNotSupportedException("There is no settings UI to open on plain .NET");
}
