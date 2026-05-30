using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Bluez;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanL2Cap
{
    public IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure) => Observable.Create<L2CapChannel>(ob =>
    {
        var cts = new CancellationTokenSource();
        L2CapSocket.L2CapHandle? handle = null;

        _ = Task.Run(async () =>
        {
            try
            {
                var bdAddr = await this.device.GetAddressAsync(cts.Token).ConfigureAwait(false);
                var addrTypeStr = await this.device.GetAddressTypeAsync(cts.Token).ConfigureAwait(false);
                var addrType = addrTypeStr == "random" ? L2CapSocket.BDADDR_LE_RANDOM : L2CapSocket.BDADDR_LE_PUBLIC;

                handle = L2CapSocket.Connect(bdAddr, addrType, psm, secure);
                var rxSubj = new Subject<byte[]>();
                var local = handle;

                // Pump receive on a dedicated thread — kernel read() blocks.
                _ = Task.Run(() =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            var frame = local.Receive();
                            if (frame == null) break;
                            rxSubj.OnNext(frame);
                        }
                        rxSubj.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        rxSubj.OnError(ex);
                    }
                });

                ob.OnNext(new L2CapChannel(
                    psm,
                    bdAddr,
                    data => Observable.FromAsync(ct => local.SendAsync(data, ct)),
                    rxSubj,
                    () =>
                    {
                        cts.Cancel();
                        local.Dispose();
                    }
                ));
                ob.OnCompleted();
            }
            catch (Exception ex)
            {
                handle?.Dispose();
                if (!cts.IsCancellationRequested)
                    ob.OnError(ex);
            }
        }, cts.Token);

        return () =>
        {
            // If the subscription is disposed before the channel is delivered, tear down what we have.
            // After delivery the channel's OnDispose owns the handle lifecycle.
            cts.Cancel();
        };
    });
}
