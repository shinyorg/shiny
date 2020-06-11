using System;


namespace Shiny.BluetoothLE.Hosting
{
    public interface IGattServiceBuilder
    {
        IGattCharacteristic AddCharacteristic(Guid uuid, Action<IGattCharacteristicBuilder> characteristicBuilder);
    }
}
