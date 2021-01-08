using System;


namespace Shiny.BluetoothLE
{
    public interface IGattService
    {
        IPeripheral Peripheral { get; }

        /// <summary>
        /// The service UUID
        /// </summary>
        string Uuid { get; }

        /// <summary>
        /// This will return a repeatable observable of discovered characteristics
        /// </summary>
        IObservable<IGattCharacteristic> DiscoverCharacteristics();

        /// <summary>
        /// Find a known characteristic
        /// </summary>
        /// <param name="characteristicId"></param>
        /// <returns></returns>
        IObservable<IGattCharacteristic> GetKnownCharacteristic(string characteristicId);
    }
}
