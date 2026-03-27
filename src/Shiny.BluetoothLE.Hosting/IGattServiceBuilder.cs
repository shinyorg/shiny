using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Builds a GATT service by adding characteristics
/// </summary>
public interface IGattServiceBuilder
{
    /// <summary>
    /// Adds a characteristic to the service
    /// </summary>
    /// <param name="uuid">The characteristic UUID</param>
    /// <param name="characteristicBuilder">Action to configure the characteristic</param>
    /// <returns>The created GATT characteristic</returns>
    IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder);
}
