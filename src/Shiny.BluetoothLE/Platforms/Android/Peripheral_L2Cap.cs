using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Infrastructure;

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

        // BluetoothSocket.Connect() blocks until the channel opens or fails, so it runs off the subscribing thread and
        // disposing the subscription closes the socket - the documented way to abort a blocking Connect(). Once the
        // channel is handed to the subscriber it owns the socket, and disposing (which Rx also does on completion)
        // leaves it open.
        var gate = new object();
        BluetoothSocket? socket = null;
        var disposed = false;
        var handedOff = false;

        Task.Run(() =>
        {
            BluetoothSocket? created = null;
            try
            {
                created = secure
                    ? this.Native.CreateL2capChannel(psm)
                    : this.Native.CreateInsecureL2capChannel(psm);

                if (created == null)
                    throw new BleException("Android could not create an L2CAP socket");

                lock (gate)
                {
                    socket = created;
                    if (disposed)
                    {
                        created.Dispose();
                        return;
                    }
                }

                created.Connect();

                lock (gate)
                {
                    if (disposed)
                        return;

                    handedOff = true;
                }

                ob.OnNext(new L2CapChannel(
                    psm,
                    this.Native.Address!,
                    data => ObservableTask.FromAsync(ct => created.OutputStream!.WriteAsync(data, 0, data.Length, ct)),
                    created.ListenForData(),
                    () => created.Dispose()
                ));
                ob.OnCompleted();
            }
            catch (Exception ex)
            {
                created?.Dispose();

                // a Connect() aborted by disposal throws - nobody is listening for it any more
                var report = false;
                lock (gate)
                    report = !disposed;

                if (report)
                    ob.OnError(ex);
            }
        });

        return () =>
        {
            BluetoothSocket? toClose = null;
            lock (gate)
            {
                disposed = true;
                if (!handedOff)
                    toClose = socket;
            }

            try
            {
                toClose?.Close();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error closing L2CAP socket while cancelling the connect");
            }
        };
    });
}
