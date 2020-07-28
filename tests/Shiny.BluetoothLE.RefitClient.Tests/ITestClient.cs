using System;
using System.Reactive;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.RefitClient.Tests
{
    public interface ITestClient
    {
        //  TODO: only async or observables

        // TODO: transactional (possibly auth signed writes)
        // write only
        [Characteristic("123", "456")]
        Task WriteRaw(byte[] data);

        // transactional
        [Characteristic("123", "456")]
        IObservable<Unit> WriteRawObservable(byte[] data);  // unit is equivalent to void/write only

        // secure reads?
        [Characteristic("123", "456")]
        Task<byte[]> ReadRaw();

        [Characteristic("123", "456")]
        IObservable<byte[]> ReadRawObservable();

        //// TODO: transactional?
        //[Characteristic("123", "456")]
        //IObservable<Unit or Progress?, transactional> WriteStream(Stream stream);

        [Characteristic("123", "456")]
        IObservable<CharacteristicGattResult> Notify();

        // TODO: Unit equivalent of void
        // TODO: secure read/writes/notifys?
        // TODO: notifys as Indicate?
        // TODO: allow for generics instead of object
        // TODO: RX/TX Flows?
    }
}
