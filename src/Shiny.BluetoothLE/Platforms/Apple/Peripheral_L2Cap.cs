using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanL2Cap
{
    // didOpenL2CAPChannel carries no PSM when it fails, so opens are serialized - otherwise one PSM's error completed
    // another's open. WaitForOperation ends the wait when the link drops instead of leaving it pending forever.
    readonly SemaphoreSlim l2capLane = new(1);

    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => ObservableTask.FromAsync(async ct =>
    {
        await this.l2capLane.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // this is the native delegate - subscribing to CBPeripheral.DidOpenL2CapChannel would replace it
            // ("Event registration is overwriting existing delegate") and cut off every other callback
            var task = this.WaitForOperation(
                this.l2capSubj.Where(x => x.Channel == null || x.Channel.Psm == psm),
                ct
            );
            this.Native.OpenL2CapChannel(psm);

            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw new BleException(result.Error.LocalizedDescription);

            if (result.Channel == null)
                throw new BleException($"CoreBluetooth did not open an L2CAP channel on PSM {psm}");

            return result.Channel.ToL2CapChannel();
        }
        finally
        {
            this.l2capLane.Release();
        }
    });


    readonly Subject<(CBL2CapChannel? Channel, NSError? Error)> l2capSubj = new();
    public override void DidOpenL2CapChannel(CBPeripheral peripheral, CBL2CapChannel? channel, NSError? error)
        => this.l2capSubj.OnNext((channel, error));
}
