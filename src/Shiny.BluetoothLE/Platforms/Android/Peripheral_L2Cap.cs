//namespace Shiny.BluetoothLE;


//public partial class Peripheral : IL2Cap
//{
//    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(ob =>
//    {
//        if (!OperatingSystemShim.IsAndroidVersionAtLeast(23))
//            throw new InvalidOperationException("L2Cap requires Android API Level 23+");

//        var socket = secure
//            ? this.Native.CreateL2capChannel(psm)
//            : this.Native.CreateInsecureL2capChannel(psm);

//        ob.Respond(new L2CapChannel(
//            psm,
//            data => Observable.FromAsync(ct => socket.InputStream!.WriteAsync(data, 0, data.Length, ct)),
//            socket.ListenForData(),
//            () => socket.Dispose()
//        ));

//        return Disposable.Empty;
//    });
//}