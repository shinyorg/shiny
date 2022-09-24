using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public interface IGattCharacteristicBuilder
{
    IGattCharacteristicBuilder SetNotification(
        Func<CharacteristicSubscription, Task>? onSubscribe = null,
        NotificationOptions options = NotificationOptions.Notify
    );


    IGattCharacteristicBuilder SetWrite(
        Func<WriteRequest, Task> request,
        WriteOptions options = WriteOptions.Write
    );


    IGattCharacteristicBuilder SetRead(
        Func<ReadRequest, Task<GattResult>> request,
        bool encrypted = false
    );
}
