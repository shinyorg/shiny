using AppKit;
using Foundation;

namespace Shiny.Net.Wifi;


/// <summary>
/// macOS has no airplane mode - the concept does not exist on the platform.
/// </summary>
/// <remarks>
/// Use <see cref="IWifiManager.SetRadioEnabled"/> instead, which CoreWLAN does support and which is
/// what a Mac user means by "turn the wireless off". <see cref="OpenSettings"/> opens the Network
/// pane of System Settings.
/// </remarks>
public class MacOSAirplaneMode : IAirplaneMode
{
    public bool IsSupported => false;
    public bool CanToggle => false;
    public bool IsEnabled => false;

    // never raised - there is no state here to watch
    public event EventHandler<bool>? Changed { add { } remove { } }


    public Task SetEnabled(bool enabled, CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.AirplaneModeToggle,
            "macOS has no airplane mode. Use IWifiManager.SetRadioEnabled to power the Wi-Fi radio instead"
        );


    public Task OpenSettings(CancellationToken ct = default)
    {
        var url = new NSUrl("x-apple.systempreferences:com.apple.Network-Settings.extension");
        NSWorkspace.SharedWorkspace.OpenUrl(url);
        return Task.CompletedTask;
    }
}
