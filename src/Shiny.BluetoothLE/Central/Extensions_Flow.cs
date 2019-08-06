using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;


namespace Shiny.BluetoothLE.Central
{
    public static partial class Extensions
    {
        // maybe finish bytes later on
        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="rxCharacteristicUuid"></param>
        /// <param name="txCharacteristicUuid"></param>
        /// <param name="txNextBytes"></param>
        /// <returns></returns>
        //public static IObservable<Unit> RxFlow(this IPeripheral peripheral,
        //                                       Guid serviceUuid,
        //                                       Guid rxCharacteristicUuid,
        //                                       Guid txCharacteristicUuid,
        //                                       byte[] txNextBytes,
        //                                       Stream writeStream) => Observable.Create<Unit>(ob =>
        //{
        //    var disp = new CompositeDisposable();
        //    IDisposable flowLoop = null;
        //    IGattCharacteristic tx = null;
        //    IGattCharacteristic rx = null;

        //    disp.Add(peripheral
        //        .WhenDisconnected()
        //        .Subscribe(x =>
        //        {
        //            tx = null;
        //            rx = null;
        //            flowLoop?.Dispose();
        //        })
        //    );
        //    disp.Add(peripheral
        //        .WhenKnownCharacteristicsDiscovered(serviceUuid, txCharacteristicUuid, rxCharacteristicUuid)
        //        .Subscribe(x =>
        //        {
        //            if (x.Uuid.Equals(txCharacteristicUuid))
        //                tx = x;

        //            else if (x.Uuid.Equals(rxCharacteristicUuid))
        //                rx = x;

        //            // while connected & not stopped
        //            if (tx != null && rx != null)
        //            {
        //                if (rx.CanNotifyOrIndicate())
        //                {
        //                    flowLoop = rx
        //                        .WhenNotificationReceived()
        //                        .Subscribe(
        //                            y =>
        //                            {
        //                                writeStream.Write(y.Data, 0, y.Data.Length);

        //                                // don't need to await this will progress stream
        //                                tx.Write(txNextBytes).Subscribe();
        //                                //ob.OnNext(Unit.Default);
        //                            },
        //                            ob.OnError
        //                        );
        //                }
        //                else
        //                {
        //                    // TODO: need state hooks so this keeps going like notification pattern
        //                    flowLoop = Observable
        //                        .While(
        //                            () => peripheral.IsConnected() && !disp.IsDisposed,
        //                            Observable.FromAsync(async ct =>
        //                            {
        //                                await tx.Write(txNextBytes).ToTask(ct);
        //                                var result = await rx.Read().ToTask(ct);
        //                                writeStream.Write(result.Data, 0, result.Data.Length);
        //                            })
        //                        )
        //                        .Subscribe(_ => { }, ob.OnError);
        //                }
        //            }
        //        })
        //    );

        //    return () =>
        //    {
        //        disp.Dispose();
        //        flowLoop?.Dispose();
        //    };
        //});


        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="ch">The characteristic to write on</param>
        /// <param name="stream">The stream to send</param>
        public static IObservable<BleWriteSegment> BlobWrite(this IGattCharacteristic ch, Stream stream)
            => Observable.Create<BleWriteSegment>(async (ob, ct) =>
            {
                var trans = ch.Service.Peripheral.TryBeginTransaction() ?? new VoidGattReliableWriteTransaction();
                using (trans)
                {
                    var mtu = ch.Service.Peripheral.MtuSize;
                    var buffer = new byte[mtu];
                    var read = stream.Read(buffer, 0, buffer.Length);
                    var pos = read;
                    var len = Convert.ToInt32(stream.Length);

                    while (!ct.IsCancellationRequested && read > 0)
                    {
                        await trans
                            .Write(ch, buffer)
                            .ToTask(ct)
                            .ConfigureAwait(false);

                        //if (this.Value != buffer)
                        //{
                        //    trans.Abort();
                        //    throw new GattReliableWriteTransactionException("There was a mismatch response");
                        //}
                        var seg = new BleWriteSegment(buffer, pos, len);
                        ob.OnNext(seg);

                        read = stream.Read(buffer, 0, buffer.Length);

                        if (read > 0 && read < buffer.Length)
                        {
                            for (var index = read; index < buffer.Length; index++)
                            {
                                buffer[index] = 0;
                            }
                        }

                        pos += read;
                    }
                    await trans.Commit();
                }
                ob.OnCompleted();

                return trans;
            });
    }
}
