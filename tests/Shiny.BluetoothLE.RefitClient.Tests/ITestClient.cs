using System;
using System.IO;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.RefitClient
{
    [Service("123")]
    public interface ITestClient
    {
        [Characteristic("456")]
        Task Write(object value);

        [Characteristic("456")]
        IObservable<object> WriteObservable(object value);

        // TODO: return type?
        [Characteristic("456")]
        IObservable<object> WriteObservableRaw(byte[] value);

        // TODO: transactional?
        [Characteristic("123")]
        IObservable<object> WriteStream(Stream stream);


        [Characteristic("456")]
        Task WriteRaw(byte[] bytes);


        [Characteristic("456")]
        Task<byte[]> ReadRaw();

        [Characteristic("456")]
        IObservable<byte> ReadObservableRaw();


        [Characteristic("456")]
        IObservable<object> Notify();

        // TODO: Unit equivalent of void
        // TODO: secure read/writes/notifys?
        // TODO: notifys as Indicate?
        // TODO: allow for generics instead of object
        // TODO: RX/TX Flows?
    }
}
