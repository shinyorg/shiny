using System.Text;
using Sample.Shared.Maui.BleHosting;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.Pages.BLE;


/// <summary>
/// The source-generated counterpart to <see cref="BleHostingViewModel"/>. Everything the other page
/// builds by hand with <c>AddService(...)</c> lives in attributes on the classes under
/// <c>BleHosting/</c>; this page only turns the whole set on and off.
/// </summary>
[ShellMap<BleHostingGeneratedPage>("blehostinggen")]
public partial class BleHostingGeneratedViewModel(
    IBleHostingManager hostingManager,
    IServiceProvider services,
    SampleGeneratedGattService gattService,
    SampleEchoStreamService streamService,
    SampleBleHostingActivity activity
) : ObservableObject, IDisposable
{
    BleHostedServiceSession? session;
    int ticks;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HostText))]
    bool isHosting;

    [ObservableProperty] string localName = "ShinyGenerated";
    [ObservableProperty] string tickPayload = "Tick";
    [ObservableProperty] string psm = "(not listening)";
    [ObservableProperty] int ticksSent;

    public SampleBleHostingActivity Activity => activity;

    public string HostText => this.IsHosting ? "Stop Hosting" : "Start Hosting";


    [RelayCommand]
    async Task ToggleHosting()
    {
        if (this.IsHosting)
        {
            await this.Stop();
            return;
        }

        var access = await hostingManager.RequestAccess();
        if (access != AccessState.Available)
        {
            activity.Status = $"Access denied: {access}";
            return;
        }

        // generated: resolves every [BleService] / [L2CapService] from DI, opens the L2CAP listener,
        // then issues one AddService per distinct service UUID
        this.session = await hostingManager.AttachBleHostedServices(services);
        await hostingManager.StartBleHostedAdvertising(this.LocalName);

        this.Psm = streamService.IsListening ? streamService.Psm.ToString() : "(unavailable)";
        this.IsHosting = true;
        activity.Status = $"Hosting as '{this.LocalName}'";
    }


    [RelayCommand]
    async Task SendTick()
    {
        if (!gattService.HasTickerSubscribers)
        {
            activity.Status = "No subscribers on the ticker characteristic";
            return;
        }

        var count = Interlocked.Increment(ref this.ticks);
        await gattService.NotifyTicker(Encoding.UTF8.GetBytes($"{this.TickPayload} #{count}"));

        this.TicksSent = count;
        activity.Status = $"Notified {gattService.TickerSubscribers.Count} central(s)";
    }


    async Task Stop()
    {
        hostingManager.StopAdvertising();

        if (this.session != null)
        {
            // cancels in-flight handlers, closes the L2CAP listener, removes the GATT services
            await this.session.DisposeAsync();
            this.session = null;
        }

        this.IsHosting = false;
        this.Psm = "(not listening)";
        activity.Subscribers = 0;
        activity.Status = "Stopped";
    }


    public void Dispose()
    {
        if (this.IsHosting)
            _ = this.Stop();
    }
}
