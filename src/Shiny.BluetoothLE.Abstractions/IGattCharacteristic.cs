using System;


namespace Shiny.BluetoothLE
{
    public interface IGattCharacteristic
    {
        IGattService Service { get; }
        string Uuid { get; }
        bool IsNotifying { get; }
        CharacteristicProperties Properties { get; }

        /// <summary>
        /// Subscribe to notifications (or indications if available) - once all subscriptions are cleared, the characteristic is unsubscribed
        /// </summary>
        /// <param name="sendHookEvent">This will send an event when the notification gets hooked for the first time if true, otherwise it is skipped</param>
        /// <param name="useIndicationIfAvailable">If true and indication is available, it will be used</param>
        /// <returns></returns>
        IObservable<CharacteristicGattResult> Notify(bool sendHookEvent = false, bool useIndicationIfAvailable = false);

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
