using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;

namespace Shiny.BluetoothLE;


public static class Extensions
{
    public static IObservable<byte[]> ListenForData(this BluetoothSocket socket) => Observable.Create<byte[]>(ob =>
    {
        var cts = new CancellationTokenSource();

        _ = Task.Run(() =>
        {
            var buffer = new byte[8192];

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var read = socket.InputStream!.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    var payload = new byte[read];
                    Array.Copy(buffer, 0, payload, 0, read);
                    ob.OnNext(payload);
                }
                ob.OnCompleted();
            }
            catch (Exception ex) when (!cts.IsCancellationRequested)
            {
                ob.OnError(ex);
            }
        });

        return () => cts.Cancel();
    });


    public static BluetoothAdapter? GetBluetoothAdapter(this AndroidPlatform platform)
        => platform.GetSystemService<BluetoothManager>(Context.BluetoothService).Adapter;


    public static AccessState GetAccessState(this BluetoothManager bluetoothManager)
    {
        var ad = bluetoothManager?.Adapter;
        if (ad == null)
            return AccessState.NotSupported;

        if (!ad.IsEnabled)
            return AccessState.Disabled;

        return ad.State.FromNative();
    }


    public static AccessState FromNative(this State state) => state switch
    {
        var x when
            x == State.Off ||
            x == State.TurningOff ||
            x == State.Disconnecting ||
            x == State.Disconnected
                => AccessState.Disabled,

        var x when
            x == State.On ||
            x == State.Connected
                => AccessState.Available,

        _ => AccessState.Unknown
    };


    public static ConnectionState ToStatus(this ProfileState state) => state switch
    {
        ProfileState.Connected => ConnectionState.Connected,
        ProfileState.Connecting => ConnectionState.Connecting,
        ProfileState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };
}