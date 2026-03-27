using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Builds a GATT characteristic by configuring read, write, and notification handlers
/// </summary>
public interface IGattCharacteristicBuilder
{
    /// <summary>
    /// Configures notification/indication support for the characteristic
    /// </summary>
    /// <param name="onSubscribe">Optional callback when a central subscribes or unsubscribes</param>
    /// <param name="options">The notification type (notify or indicate)</param>
    /// <returns>The builder for chaining</returns>
    IGattCharacteristicBuilder SetNotification(
        Func<CharacteristicSubscription, Task>? onSubscribe = null,
        NotificationOptions options = NotificationOptions.Notify
    );


    /// <summary>
    /// Configures write support for the characteristic
    /// </summary>
    /// <param name="request">The write request handler</param>
    /// <param name="options">The write type (write with response or write without response)</param>
    /// <returns>The builder for chaining</returns>
    IGattCharacteristicBuilder SetWrite(
        Func<WriteRequest, Task> request,
        WriteOptions options = WriteOptions.Write
    );


    /// <summary>
    /// Configures read support for the characteristic
    /// </summary>
    /// <param name="request">The read request handler</param>
    /// <param name="encrypted">Whether the read requires encryption</param>
    /// <returns>The builder for chaining</returns>
    IGattCharacteristicBuilder SetRead(
        Func<ReadRequest, Task<GattResult>> request,
        bool encrypted = false
    );
}
