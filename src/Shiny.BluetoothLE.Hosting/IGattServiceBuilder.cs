using System;

namespace Shiny.BluetoothLE.Hosting;


public interface IGattServiceBuilder
{
    IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder);
}
