using System;
using System.Reactive;


namespace Shiny.BluetoothLE
{
    public interface IStream
    {
        bool IsDataAvailable { get; }
        void Open();
        void Close();
        bool CanRead { get; }
        bool CanWrite { get; }

        bool IsOpen { get; }
        //public int Read(byte[] buffer, int offset, int count) => 0;


        IObservable<Unit> Write(byte[] buffer);
        IObservable<Unit> Write(byte[] buffer, int offeset, int count);
    }
}
