using System;


namespace Shiny.BluetoothLE.Peripherals
{
    public interface IGattServiceBuilder
    {
        IGattCharacteristic AddCharacteristic(Guid uuid, Action<IGattCharacteristicBuilder> characteristicBuilder);
    }
}
