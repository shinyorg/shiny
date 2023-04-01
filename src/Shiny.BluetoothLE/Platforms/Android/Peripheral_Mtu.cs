using System;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral //: ICanRequestMtu
{
    int currentMtu = 20;
    public int Mtu => this.currentMtu;
//public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status) { }

//    int currentMtu = 20;
//    public IObservable<int> RequestMtu(int size) => this.Context.Invoke(Observable.Create<int>(ob =>
//    {
//        this.AssertConnection();
//        var sub = this.WhenMtuChanged().Skip(1).Take(1).Subscribe(ob.Respond);
//        this.Context.Gatt!.RequestMtu(size);
//        return sub;
//    }));


//    public IObservable<int> WhenMtuChanged() => this.Context
//        .Callbacks
//        .MtuChanged
//        .Where(x => x.IsSuccessful)
//        .Select(x =>
//        {
//            this.currentMtu = x.Mtu;
//            return x.Mtu;
//        })
//        .StartWith(this.currentMtu);


//    public override int MtuSize => this.currentMtu;
}