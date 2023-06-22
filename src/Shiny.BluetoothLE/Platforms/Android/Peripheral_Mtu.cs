using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanRequestMtu
{
    public int Mtu { get; private set; } = 20;


    Subject<(int Mtu, GattStatus Status)>? mtuSubj;
    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, GattStatus status)
    { 
        this.Mtu = mtu;
        this.mtuSubj?.OnNext((mtu, status));
    }


    public IObservable<int> RequestMtu(int requestValue) => Observable.Create<int>(ob =>
    {
        this.AssertConnection();

        this.mtuSubj ??= new();
        var disposable = this.mtuSubj.Subscribe(x =>
        {
            if (x.Status == GattStatus.Success)
                ob.Respond(x.Mtu);
            else
                ob.OnError(ToException("MTU Requested Failed", x.Status));
        });

        this.Gatt!.RequestMtu(requestValue);
        return disposable;
    });
}

