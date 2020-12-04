using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public interface ICanDoTransactions
    {
        IGattReliableWriteTransaction BeginReliableWriteTransaction();
    }


    public static class Feature_Transactions
    {
        public static IGattReliableWriteTransaction BeginTransaction(this IPeripheral peripheral)
            => ((ICanDoTransactions)peripheral).BeginReliableWriteTransaction();


        public static IGattReliableWriteTransaction? TryBeginTransaction(this IPeripheral peripheral)
        {
            if (peripheral is ICanDoTransactions trans)
                return trans.BeginReliableWriteTransaction();

            return null;
        }

        public static bool IsReliableTransactionsAvailable(this IPeripheral peripheral) => peripheral is ICanDoTransactions;


        public static IObservable<BleWriteSegment> BlobWriteTransaction(this IGattCharacteristic ch, Stream stream) => Observable.Create<BleWriteSegment>(async (ob, ct) =>
        {
            var trans = ch.Service.Peripheral.TryBeginTransaction();
            using (trans)
            {
                var mtu = ch.Service.Peripheral.MtuSize;
                var buffer = new byte[mtu];
                var read = stream.Read(buffer, 0, buffer.Length);
                var pos = read;
                var len = Convert.ToInt32(stream.Length);
                var remaining = 0;

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

                    remaining = len - pos;
                    if (remaining > 0 && remaining < mtu)
                    {
                        // readjust buffer -- we don't want to send extra garbage 
                        buffer = new byte[remaining];
                    }

                    read = stream.Read(buffer, 0, buffer.Length);
                    pos += read;
                }
                await trans.Commit();
            }
            ob.OnCompleted();

            return trans;
        });
    }
}
