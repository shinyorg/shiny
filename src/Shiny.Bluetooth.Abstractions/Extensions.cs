using System;
using System.Reactive.Linq;


namespace Shiny.Bluetooth
{
    public static class Extensions
    {
        /// <summary>
        /// Reads stream as soon as data becomes available
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static IObservable<byte[]> WhenDataRead(this IBluetoothDevice device, uint bufferSize = 1024) => device
            .WhenDataAvailable()
            .Select(x => device.Read(bufferSize))
            .Switch();


        /// <summary>
        /// Read the stream without supplying your own buffer
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static IObservable<byte[]> Read(this IBluetoothDevice device, uint bufferSize = 1024)
        {
            var buffer = new byte[bufferSize];
            return device
                .Read(buffer, bufferSize)
                .Select(_ => buffer);
        }
    }
}
