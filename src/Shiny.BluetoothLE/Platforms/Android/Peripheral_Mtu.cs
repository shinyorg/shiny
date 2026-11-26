using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanRequestMtu
{
    public int Mtu { get; private set; } = BleConstants.DefaultPayloadSize;


    Subject<(int Mtu, GattStatus Status)>? mtuSubj;
    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, GattStatus status)
    {
        // a failed exchange leaves the link at its previous MTU
        if (status == GattStatus.Success)
            this.Mtu = mtu - BleConstants.AttHeaderSize;

        this.mtuSubj?.OnNext((mtu - BleConstants.AttHeaderSize, status));
    }


    // An MTU exchange is a GATT operation like any other: BluetoothGatt refuses it (returns false) while another is
    // in flight, so it goes through the operation queue, and a refusal fails at once instead of at the caller's timeout.
    public IObservable<int> RequestMtu(int requestValue) => this.operations.QueueToObservable(async ct =>
    {
        this.AssertConnection();

        this.mtuSubj ??= new();
        var task = this.WaitForOperation(this.mtuSubj, ct);

        if (!this.Gatt!.RequestMtu(requestValue))
            throw new BleException("Android GATT refused the MTU request");

        var result = await task.ConfigureAwait(false);
        if (result.Status != GattStatus.Success)
            throw ToException("MTU Requested Failed", result.Status);

        return result.Mtu;
    });
}
