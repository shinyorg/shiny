using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shiny.Net.Wifi.NetworkManager;
using Tmds.DBus.Protocol;

namespace Shiny.Net.Wifi;


/// <summary>
/// Linux airplane mode, expressed as NetworkManager's wireless and WWAN kill switches.
/// </summary>
/// <remarks>
/// <para>Linux has no single airplane mode flag; what desktop environments label "Airplane Mode" is
/// exactly this - NetworkManager's <c>WirelessEnabled</c> and <c>WwanEnabled</c> soft-blocks, which
/// it in turn drives through rfkill. Airplane mode reads as on when both are off.</para>
/// <para>Setting either goes through polkit
/// (<c>org.freedesktop.NetworkManager.enable-disable-wifi</c> and <c>…-wwan</c>). A hardware kill
/// switch overrides all of it and cannot be cleared from software.</para>
/// </remarks>
public class LinuxAirplaneMode(ILogger<LinuxAirplaneMode> logger) : IAirplaneMode
{
    readonly NmClient client = new();
    IDisposable? watcher;
    int subscriberCount;
    bool cached;

    public bool IsSupported => true;
    public bool CanToggle => true;


    public bool IsEnabled
    {
        get
        {
            // D-Bus is async and this is not; the value is refreshed by the watcher once subscribed
            var read = Task.Run(() => this.Read(CancellationToken.None));
            return read.Wait(TimeSpan.FromSeconds(5)) ? read.Result : this.cached;
        }
    }


    async Task<bool> Read(CancellationToken ct)
    {
        try
        {
            var wireless = await this.client.GetWirelessEnabled(ct).ConfigureAwait(false);
            var wwan = await this.client.GetWwanEnabled(ct).ConfigureAwait(false);
            this.cached = !wireless && !wwan;
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not read the NetworkManager radio state");
        }
        return this.cached;
    }


    event EventHandler<bool>? changed;
    public event EventHandler<bool>? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
                _ = this.StartListening();
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
            {
                this.watcher?.Dispose();
                this.watcher = null;
            }
        }
    }


    async Task StartListening()
    {
        try
        {
            await this.Read(CancellationToken.None).ConfigureAwait(false);
            this.watcher = await this.client
                .WatchPropertiesChanged(() => _ = this.OnPropertiesChanged())
                .ConfigureAwait(false);

            logger.WatcherStarted("NetworkManager PropertiesChanged");
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not subscribe to NetworkManager property changes");
        }
    }


    async Task OnPropertiesChanged()
    {
        var previous = this.cached;
        var current = await this.Read(CancellationToken.None).ConfigureAwait(false);

        // NetworkManager emits PropertiesChanged for everything it owns, most of it unrelated
        if (current != previous)
            this.changed?.Invoke(this, current);
    }


    public async Task SetEnabled(bool enabled, CancellationToken ct = default)
    {
        try
        {
            await this.client.SetWirelessEnabled(!enabled, ct).ConfigureAwait(false);
            await this.client.SetWwanEnabled(!enabled, ct).ConfigureAwait(false);
            logger.RadioToggled(!enabled);
        }
        catch (DBusExceptionBase ex)
        {
            throw new WifiPermissionException($"NetworkManager refused to change the radio state - {ex.Describe()}. This needs the polkit actions org.freedesktop.NetworkManager.enable-disable-wifi and .enable-disable-wwan", ex);
        }
    }


    /// <remarks>
    /// There is no desktop-agnostic settings URL on Linux, so this hands the request to xdg-open
    /// and lets the desktop environment resolve it. GNOME and KDE both register the handler; a bare
    /// window manager will not, and the call fails rather than silently doing nothing.
    /// </remarks>
    public Task OpenSettings(CancellationToken ct = default)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = "gnome-control-center://network",
                UseShellExecute = false
            });
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new WifiException("Could not open the network settings - no xdg-open handler is registered on this desktop", ex);
        }
    }
}
