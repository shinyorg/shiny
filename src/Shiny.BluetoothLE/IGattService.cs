using System;
using System.Collections.Generic;


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
        IObservable<IList<IGattCharacteristic>> GetCharacteristics();

        /// <summary>
        /// Find a known characteristic
        /// </summary>
        /// <param name="characteristicId"></param>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicId, bool throwIfNotFound = false);
    }
}
