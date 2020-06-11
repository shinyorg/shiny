using System;


namespace Shiny.BluetoothLE.Hosting
{
    public interface IGattCharacteristicBuilder
    {
        IGattCharacteristicBuilder SetNotification(Action<CharacteristicSubscription> onSubscribe = null, NotificationOptions options = NotificationOptions.Notify);
        IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> request, WriteOptions options = WriteOptions.Write);
        IGattCharacteristicBuilder SetRead(Func<ReadRequest, ReadResult> request, bool encrypted = false);
    }
}
