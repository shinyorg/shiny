using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanL2Cap
{
    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(ob =>
    {
        // this is the native delegate - subscribing to CBPeripheral.DidOpenL2CapChannel would replace it
        // ("Event registration is overwriting existing delegate") and cut off every other callback
        var sub = this.l2capSubj
            .Where(x => x.Channel == null || x.Channel.Psm == psm)
            .Take(1)
            .Subscribe(x =>
            {
                if (x.Error != null)
                {
                    ob.OnError(new BleException(x.Error.LocalizedDescription));
                }
                else
                {
                    ob.OnNext(x.Channel!.ToL2CapChannel());
                    ob.OnCompleted();
                }
            });

        this.Native.OpenL2CapChannel(psm);
        return sub;
    });


    readonly Subject<(CBL2CapChannel? Channel, NSError? Error)> l2capSubj = new();
    public override void DidOpenL2CapChannel(CBPeripheral peripheral, CBL2CapChannel? channel, NSError? error)
        => this.l2capSubj.OnNext((channel, error));
}
