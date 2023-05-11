using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanRequestMtu
{
    public int Mtu { get; private set; } = 20;


    readonly Subject<(int Mtu, GattStatus Status)> mtuSubj = new();
    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, GattStatus status)
    { 
        this.Mtu = mtu;
        this.mtuSubj.OnNext((mtu, status));
    }


    public IObservable<int> RequestMtu(int requestValue) => Observable.Create<int>(ob =>
    {
        this.AssertConnection();

        var disposable = this.mtuSubj.Subscribe(x =>
        {
            if (x.Status == GattStatus.Success)
                ob.Respond(x.Mtu);
            else
                ob.OnError(new InvalidOperationException("MTU Requested Failed - " + x.Status));
        });

        this.Gatt!.RequestMtu(requestValue);
        return disposable;
    });
}

