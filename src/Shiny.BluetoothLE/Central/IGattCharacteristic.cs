using System;
using System.IO;


namespace Shiny.BluetoothLE.Central
{
    public interface IGattCharacteristic
    {
        IGattService Service { get; }
        Guid Uuid { get; }
        string Description { get; }
        bool IsNotifying { get; }
        CharacteristicProperties Properties { get; }
        byte[]? Value { get; }

        /// <summary>
        /// Subscribe to notifications (or indications if available) - once all subscriptions are cleared, the characteristic is unsubscribed
        /// </summary>
        /// <param name="useIndicationIfAvailable">If true and indication is available, it will be used</param>
        /// <returns></returns>
        IObservable<CharacteristicGattResult> Notify(bool useIndicationIfAvailable = false);

        /// <summary>
        /// Discovers descriptors for this characteristic
        /// </summary>
        /// <returns></returns>
        IObservable<IGattDescriptor> DiscoverDescriptors();

        /// <summary>
        /// Writes the value to the remote characteristic
        /// </summary>
        /// <param name="value">The bytes to send</param>
        /// <param name="withResponse">Write with or without response</param>
        IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse = true);

        /// <summary>
        /// Read characteristic remote value
        /// </summary>
        /// <returns></returns>
        IObservable<CharacteristicGattResult> Read();
    }
}
