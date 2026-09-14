using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Represents a GATT characteristic hosted on the local BLE peripheral
/// </summary>
public interface IGattCharacteristic
{
    /// <summary>
    /// Gets the characteristic UUID
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// Gets the characteristic properties (read, write, notify, etc.)
    /// </summary>
    CharacteristicProperties Properties { get; }

    /// <summary>
    /// Sends a notification to subscribed centrals
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <param name="centrals">Specific centrals to notify, or all subscribed if empty</param>
    Task Notify(byte[] data, params IPeripheral[] centrals);

    /// <summary>
    /// Sends a notification to subscribed centrals. On Apple platforms the returned task waits while the
    /// transmit queue is full, and faults if Bluetooth stops being powered on before the value is queued
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <param name="cancellationToken">Stops waiting for room in the transmit queue</param>
    /// <param name="centrals">Specific centrals to notify, or all subscribed if empty</param>
    Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals);

    /// <summary>
    /// Gets the list of centrals currently subscribed to notifications
    /// </summary>
    IReadOnlyList<IPeripheral> SubscribedCentrals { get; }
    //IGattDescriptor AddDescriptor(Guid uuid);
    //IReadOnlyList<IGattDescriptor> Descriptors { get; }
}
