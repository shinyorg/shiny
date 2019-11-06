using System;
using System.Reactive.Linq;
using Shiny.Logging;


namespace Shiny.BluetoothLE.Central
{
    public static class CharacteristicExtensions
    {
        /// <summary>
        /// Enables notifications and hooks it for discovered characteristic.  When subscription is disposed, it will also clean up.
        /// </summary>
        /// <param name="characteristic"></param>
        /// <param name="useIndicationIfAvailable"></param>
        /// <param name="autoCleanup"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> RegisterAndNotify(this IGattCharacteristic characteristic, bool useIndicationIfAvailable = false, bool autoCleanup = true)
        {
            var ob = characteristic
                .EnableNotifications(useIndicationIfAvailable)
                .Select(x => x.Characteristic.WhenNotificationReceived())
                .Switch();

            if (autoCleanup)
            {
                ob = ob.Finally(() => characteristic
                    .DisableNotifications()
                    .Where(x => x.Characteristic.Service.Peripheral.Status == ConnectionState.Connected)
                    .Subscribe(
                        _ => { },
                        ex => Log.Write(ex)
                    )
                );
            }

            return ob;
        }


        public static IObservable<CharacteristicGattResult> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
            => Observable
                .Interval(timeSpan)
                .Select(_ => character.Read())
                .Switch();


        public static bool CanRead(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Read);
        public static bool CanWriteWithResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Write);
        public static bool CanWriteWithoutResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
        public static bool CanWrite(this IGattCharacteristic ch)
            => ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse) || ch.Properties.HasFlag(CharacteristicProperties.Write);


        public static bool CanNotifyOrIndicate(this IGattCharacteristic ch) => ch.CanNotify() || ch.CanIndicate();


        public static bool CanNotify(this IGattCharacteristic ch) =>
            ch.Properties.HasFlag(CharacteristicProperties.Notify) ||
            ch.Properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired) ||
            ch.CanIndicate();


        public static bool CanIndicate(this IGattCharacteristic ch) =>
            ch.Properties.HasFlag(CharacteristicProperties.Indicate) ||
            ch.Properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired);


        public static void AssertWrite(this IGattCharacteristic characteristic, bool withResponse)
        {
            if (!characteristic.CanWrite())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support writes");

            if (withResponse && !characteristic.CanWriteWithResponse())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support writes with response");
        }


        public static void AssertRead(this IGattCharacteristic characteristic)
        {
            if (!characteristic.CanRead())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support reads");
        }


        public static void AssertNotify(this IGattCharacteristic characteristic)
        {
            if (!characteristic.CanNotify())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support notifications");
        }
    }
}
