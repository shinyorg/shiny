using Microsoft.Extensions.Logging;
using Windows.Devices.Radios;

namespace Shiny.Net.Wifi;


/// <summary>
/// Windows airplane mode, approximated by the state of every radio on the machine.
/// </summary>
/// <remarks>
/// <para>WinRT has no airplane mode API. What the Settings toggle does is switch every radio off
/// at once, so that is what this does - and airplane mode reads as on when no radio is left on.
/// The approximation is visible in one place: switching Wi-Fi and Bluetooth off individually makes
/// <see cref="IsEnabled"/> report true even though the Settings toggle shows off.</para>
/// <para>Needs the <c>radios</c> capability in the app manifest.</para>
/// </remarks>
public class WindowsAirplaneMode(ILogger<WindowsAirplaneMode> logger) : IAirplaneMode
{
    IReadOnlyList<Radio>? radios;
    int subscriberCount;

    public bool IsSupported => true;
    public bool CanToggle => true;


    public bool IsEnabled
    {
        get
        {
            var all = this.radios;
            if (all == null || all.Count == 0)
                return false;

            return all.All(x => x.State is RadioState.Off or RadioState.Disabled);
        }
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
                this.StopListening();
        }
    }


    public async Task SetEnabled(bool enabled, CancellationToken ct = default)
    {
        var all = await this.GetRadios(ct).ConfigureAwait(false);
        var target = enabled ? RadioState.Off : RadioState.On;

        foreach (var radio in all)
        {
            // a radio the OS has hard-disabled (a hardware switch, or group policy) cannot be
            // driven from here, and failing the whole call over it would be wrong
            if (radio.State != RadioState.Disabled)
            {
                var status = await radio.SetStateAsync(target).AsTask(ct).ConfigureAwait(false);
                if (status != RadioAccessStatus.Allowed)
                    throw new WifiPermissionException($"The '{radio.Name}' radio could not be switched - {status}. Declare the 'radios' capability in the app manifest");
            }
        }
        logger.RadioToggled(!enabled);
    }


    public Task OpenSettings(CancellationToken ct = default)
    {
        var uri = new Uri("ms-settings:network-airplanemode");
        return Windows.System.Launcher.LaunchUriAsync(uri).AsTask(ct);
    }


    async Task StartListening()
    {
        try
        {
            var all = await this.GetRadios(CancellationToken.None).ConfigureAwait(false);
            foreach (var radio in all)
                radio.StateChanged += this.OnRadioStateChanged;

            logger.WatcherStarted(nameof(Radio.StateChanged));
        }
        catch (Exception ex)
        {
            logger.WifiError(ex, "Could not subscribe to radio state changes");
        }
    }


    void StopListening()
    {
        var all = this.radios;
        if (all == null)
            return;

        foreach (var radio in all)
            radio.StateChanged -= this.OnRadioStateChanged;
    }


    void OnRadioStateChanged(Radio sender, object args) => this.changed?.Invoke(this, this.IsEnabled);


    async Task<IReadOnlyList<Radio>> GetRadios(CancellationToken ct)
    {
        if (this.radios != null)
            return this.radios;

        var access = await Radio.RequestAccessAsync().AsTask(ct).ConfigureAwait(false);
        if (access != RadioAccessStatus.Allowed)
            throw new WifiPermissionException($"Access to the machine's radios was refused - {access}. Declare the 'radios' capability in the app manifest");

        this.radios = await Radio.GetRadiosAsync().AsTask(ct).ConfigureAwait(false);
        return this.radios;
    }
}
