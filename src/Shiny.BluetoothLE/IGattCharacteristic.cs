using System;
using System.Collections.Generic;


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
        /// <param name="enable"></param>
        /// <param name="useIndicationIfAvailable">If true and indication is available, it will be used</param>
        /// <returns></returns>
        IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationIfAvailable = false);

        ///// <summary>
        ///// Subscribe to notifications (or indications if available) - once all subscriptions are cleared, the characteristic is unsubscribed
        ///// </summary>
        ///// <returns></returns>
        IObservable<GattCharacteristicResult> WhenNotificationReceived();

        /// <summary>
        /// Discovers descriptors for this characteristic
        /// </summary>
        /// <returns></returns>
        IObservable<IList<IGattDescriptor>> GetDescriptors();

        /// <summary>
        /// Writes the value to the remote characteristic
        /// </summary>
        /// <param name="value">The bytes to send</param>
        /// <param name="withResponse">Write with or without response</param>
        IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse = true);

        /// <summary>
        /// Read characteristic remote value
        /// </summary>
        /// <returns></returns>
        IObservable<GattCharacteristicResult> Read();
    }
}
