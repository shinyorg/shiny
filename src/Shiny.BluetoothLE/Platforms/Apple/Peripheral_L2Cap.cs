//using System;

//namespace Shiny.BluetoothLE;


//public partial class Peripheral : ICanL2Cap
//{
//    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(async ob =>
//    {
//        var handler = new EventHandler<CBPeripheralOpenL2CapChannelEventArgs>((sender, args) =>
//        {
//            var c = args.Channel!;

//            ob.Respond(new L2CapChannel(
//                args.Channel!.Psm,
//                data => Observable.FromAsync(ct => c.OutputStream.WriteAsync(data, 0, data.Length, ct)),
//                c.InputStream.ListenForData()
//            ));
//        });

//        this.Native.DidOpenL2CapChannel += handler;
//        this.Native.OpenL2CapChannel(psm);

//        return () => this.Native.DidOpenL2CapChannel -= handler;
//    });
//}