using Foundation;
using UIKit;

namespace Shiny.Net.Wifi;


/// <summary>
/// Airplane mode on iOS and Mac Catalyst - readable by no one, settable by no one.
/// </summary>
/// <remarks>
/// Apple exposes neither the state nor a switch. <see cref="OpenSettings"/> opens your app's own
/// settings page, which is as close as a sandboxed app can get: the <c>App-Prefs:</c> scheme that
/// deep-links to the airplane mode row is private API and gets apps rejected.
/// </remarks>
public class AppleAirplaneMode : IAirplaneMode
{
    public bool IsSupported => false;
    public bool CanToggle => false;
    public bool IsEnabled => false;

    // never raised - there is no state here to watch
    public event EventHandler<bool>? Changed { add { } remove { } }


    public Task SetEnabled(bool enabled, CancellationToken ct = default)
        => throw WifiNotSupportedException.For(
            WifiCapabilities.AirplaneModeToggle,
            "Apple exposes no airplane mode API. Send the user to Settings with OpenSettings() instead"
        );


    public Task OpenSettings(CancellationToken ct = default)
    {
        var url = new NSUrl(UIApplication.OpenSettingsUrlString);
        UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
        return Task.CompletedTask;
    }
}
