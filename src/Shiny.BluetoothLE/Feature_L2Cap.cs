//using System;


//namespace Shiny.BluetoothLE
//{
//    public interface IL2CapSupport
//    {
//        /// <summary>
//        /// Open an L2Cap socket
//        /// </summary>
//        /// <param name="psm">PSM Value</param>
//        /// <returns></returns>
//        IObservable<IChannel> OpenChannel(int psm);
//    }

//public interface IChannel : IDisposable
//{
//    Guid PeerUuid { get; }
//    int Psm { get; } //=> 0x25;

//    IStream InputStream { get; }
//    IStream OutputStream { get; }
//}
//using System;
//using System.Reactive;


//namespace Shiny.BluetoothLE
//{
//    public interface IStream
//    {
//        bool IsDataAvailable { get; }
//        void Open();
//        void Close();
//        bool CanRead { get; }
//        bool CanWrite { get; }

//        bool IsOpen { get; }
//        //public int Read(byte[] buffer, int offset, int count) => 0;


//        IObservable<Unit> Write(byte[] buffer);
//        IObservable<Unit> Write(byte[] buffer, int offeset, int count);
//    }
//}

//    public static class FeatureL2Cap
//    {
//        public static bool IsL2CapAvailable(this IPeripheral peripheral) => peripheral is IL2CapSupport;


//        public static IObservable<IChannel>? OpenChannel(this IPeripheral peripheral, int psm)
//        {
//            if (peripheral is IL2CapSupport support)
//                return support.OpenChannel(psm);

//            return null;
//        }
//    }
//}
