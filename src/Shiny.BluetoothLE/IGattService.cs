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
        /// Search for known characteristics
        /// </summary>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        IObservable<IGattCharacteristic> GetKnownCharacteristics(params string[] characteristicIds);
    }
}
