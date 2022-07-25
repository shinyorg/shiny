using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE.Hosting;


public interface IGattService
{
    string Uuid { get; }
    bool Primary { get; }
    IReadOnlyList<IGattCharacteristic> Characteristics { get; }
}
