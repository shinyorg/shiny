using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanPairPeripherals
{
    public IObservable<bool> PairingRequest(string? pin = null) => Observable.Create<bool>(ob =>
    {
        var disp = new CompositeDisposable();

        if (this.PairingStatus == PairingState.Paired)
        {
            ob.Respond(true);
        }
        else
        {
            //this.Context
            //    .ManagerContext
            //    .ListenForMe(this)
            //    .Subscribe(intent =>
            //    {

            //        switch (intent.Action)
            //        {
            //            case BluetoothDevice.ActionBondStateChanged:
            //                var prev = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraPreviousBondState, (int)Bond.None);
            //                var current = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraBondState, (int)Bond.None);

            //                if (prev == Bond.Bonding || current == Bond.Bonded)
            //                {
            //                    // it is done now
            //                    var bond = current == Bond.Bonded;
            //                    ob.Respond(bond);
            //                }

            //                break;

            //            case BluetoothDevice.ActionPairingRequest:
            //                if (!pin.IsEmpty())
            //                {
            //                    var bytes = Encoding.UTF8.GetBytes(pin);
            //                    if (!this.Native.SetPin(bytes))
            //                    {
            //                        ob.OnError(new ArgumentException("Failed to set PIN"));
            //                    }
            //                }
            //                break;
            //        }
            //    })
            //    .DisposedBy(disp);

            if (!this.Native.CreateBond())
                ob.Respond(false);
        }
        return disp;
    });


    public PairingState PairingStatus => this.Native.BondState switch
    {
        Bond.Bonded => PairingState.Paired,
        _ => PairingState.NotPaired
    };
}