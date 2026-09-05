using System.Reactive.Threading.Tasks;
using Sample.tvOS.Infrastructure;
using Shiny.BluetoothLE;

namespace Sample.tvOS.Pages;


/// <summary>
/// CoreBluetooth's central role is complete on tvOS - this is the same code an iOS app would run,
/// with no #if anywhere. The two things you cannot do here are advertise (there is no peripheral
/// role on tvOS) and scan in the background (tvOS has no bluetooth-central background mode).
/// </summary>
public class BleViewController() : ModuleViewController(
    "Shiny.BluetoothLE - central role. Scanning stops when the app is suspended: tvOS has no background Bluetooth"
)
{
    readonly HashSet<string> seen = new();
    IDisposable? scan;


    protected override void OnReady()
    {
        this.AddAction("Permissions", async () =>
        {
            var ble = Resolve<IBleManager>();
            this.Log($"current access: {ble.CurrentAccess}");
            var access = await ble.RequestAccess().ToTask();
            this.Log($"requested access: {access}");
        });

        this.AddAction("Scan", () =>
        {
            if (this.scan != null)
            {
                this.Log("already scanning");
                return Task.CompletedTask;
            }

            this.seen.Clear();
            this.ClearLog();
            this.Log("scanning...");

            var ble = Resolve<IBleManager>();
            this.scan = ble.Scan().Subscribe(
                result =>
                {
                    var id = result.Peripheral.Uuid;
                    if (this.seen.Add(id))
                        this.Log($"{result.Peripheral.Name ?? "(unnamed)"}  {result.Rssi} dBm  {id}");
                },
                ex => this.Log($"scan error: {ex.Message}")
            );
            return Task.CompletedTask;
        });

        this.AddAction("Stop", () =>
        {
            this.scan?.Dispose();
            this.scan = null;
            this.Log($"scan stopped - {this.seen.Count} peripherals seen");
            return Task.CompletedTask;
        });

        this.AddAction("Connected", () =>
        {
            var ble = Resolve<IBleManager>();
            var connected = ble.GetConnectedPeripherals().ToList();
            this.Log($"{connected.Count} connected peripheral(s)");
            foreach (var p in connected)
                this.Log($"  {p.Name ?? "(unnamed)"}  {p.Uuid}  {p.Status}");
            return Task.CompletedTask;
        });
    }


    public override void ViewDidDisappear(bool animated)
    {
        // no background scanning on tvOS, so there is nothing to be gained by leaving it running
        this.scan?.Dispose();
        this.scan = null;
        base.ViewDidDisappear(animated);
    }
}
