using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Peripherals
{
    public interface IGattService
    {
        Guid Uuid { get; }
        bool Primary { get; }
        IReadOnlyList<IGattCharacteristic> Characteristics { get; }
    }
}
