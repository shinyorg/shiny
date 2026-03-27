using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Represents a GATT service hosted on the local BLE peripheral
/// </summary>
public interface IGattService
{
    /// <summary>
    /// Gets the service UUID
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// Gets whether this is a primary service
    /// </summary>
    bool Primary { get; }

    /// <summary>
    /// Gets the characteristics belonging to this service
    /// </summary>
    IReadOnlyList<IGattCharacteristic> Characteristics { get; }
}
