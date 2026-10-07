using System;
using System.Reactive.Linq;
using CoreBluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanL2Cap
{
    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(ob =>
    {
        var handler = new EventHandler<CBPeripheralOpenL2CapChannelEventArgs>((sender, args) =>
        {
            if (args.Error != null)
            {
                ob.OnError(new BleException(args.Error.LocalizedDescription));
                return;
            }

            ob.OnNext(args.Channel!.ToL2CapChannel());
            ob.OnCompleted();
        });

        this.Native.DidOpenL2CapChannel += handler;
        this.Native.OpenL2CapChannel(psm);

        return () => this.Native.DidOpenL2CapChannel -= handler;
    });
}
