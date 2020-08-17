using System;
using System.Reactive;
using System.Threading.Tasks;
using Shiny.BluetoothLE.RefitClient;


namespace Shiny.BluetoothLE.RefitClient.Tests
{
    public interface ITestClient : IBleClient
    {
        //  TODO: only async or observables

        //// TODO: transactional (possibly auth signed writes)
        //// write only
        [BleMethod("A3D7F0FD-7805-4787-843B-C4035E6CD980", "A3D7F0FD-7805-4787-843B-C4035E6CD981")]
        Task WriteRaw(byte[] data);

        //[Guid("A3D7F0FD-7805-4787-843B-C4035E6CD983")]
        //[Guid("BEAA5B50-B4AF-45EE-8019-13C5667AE9CF")]
        //// transactional
        //[Characteristic("123", "456")]
        //IObservable<Unit> WriteRawObservable(byte[] data);  // unit is equivalent to void/write only

        //// secure reads?
        //[Characteristic("123", "456")]
        //Task<byte[]> ReadRaw();

        //[Characteristic("123", "456")]
        //IObservable<byte[]> ReadRawObservable();

        ////// TODO: transactional?
        ////[Characteristic("123", "456")]
        ////IObservable<Unit or Progress?, transactional> WriteStream(Stream stream);

        //[Characteristic("123", "456")]
        //IObservable<CharacteristicGattResult> Notify();

        // TODO: Unit equivalent of void
        // TODO: secure read/writes/notifys?
        // TODO: notifys as Indicate?
        // TODO: allow for generics instead of object
        // TODO: RX/TX Flows?
    }
}
