using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IGattService
    {
        IPeripheral Peripheral { get; }

        /// <summary>
        /// The service UUID
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        /// A general description of what the services if known
        /// </summary>
        string Description { get; }

        /// <summary>
        /// This will return a repeatable observable of discovered characteristics
        /// </summary>
        IObservable<IGattCharacteristic> DiscoverCharacteristics();

        /// <summary>
        /// Search for known characteristics
        /// </summary>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds);
    }
}
