using System;
using System.Reactive.Linq;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanL2Cap
{
    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(ob =>
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            ob.OnError(new InvalidOperationException("L2Cap requires Android API Level 29+"));
            return () => { };
        }

        BluetoothSocket? socket = null;
        try
        {
            socket = secure
                ? this.Native.CreateL2capChannel(psm)
                : this.Native.CreateInsecureL2capChannel(psm);

            socket!.Connect();

            ob.OnNext(new L2CapChannel(
                psm,
                this.Native.Address!,
                data => Observable.FromAsync(ct => socket.OutputStream!.WriteAsync(data, 0, data.Length, ct)),
                socket.ListenForData(),
                () => socket.Dispose()
            ));
            ob.OnCompleted();
        }
        catch (Exception ex)
        {
            socket?.Dispose();
            ob.OnError(ex);
        }

        return () => { };
    });
}
