using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public static partial class Extensions
    {
        internal static IObservable<IGattService?> Assert(this IObservable<IGattService?> ob, string serviceUuid, bool throwIfNotFound)
            => ob.Do(service =>
            {
                if (service == null && throwIfNotFound)
                    throw new ArgumentException($"No service found - {serviceUuid}");
            });


        internal static IObservable<IGattCharacteristic?> Assert(this IObservable<IGattCharacteristic?> ob, string serviceUuid, string characteristicUuid, bool throwIfNotFound)
            => ob.Do(ch =>
            {
                if (ch == null && throwIfNotFound)
                    throw new ArgumentException($"No characteristic found - {serviceUuid}/{characteristicUuid}");
            });


        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IList<IGattCharacteristic>> GetCharacteristicsByService(this IPeripheral peripheral, string serviceUuid) =>
            peripheral
                .GetKnownService(serviceUuid, true)
                .Select(x => x.GetCharacteristics())
                .Switch();


        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="useIndicationsIfAvailable"></param>
        /// <returns></returns>
        public static IObservable<GattCharacteristicResult> WhenConnectedNotify(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = false)
            => peripheral
                .WhenConnected()
                .Select(x => x.GetKnownCharacteristic(serviceUuid, characteristicUuid, true))
                .Switch()
                .Select(x => x.Notify(useIndicationsIfAvailable))
                .Switch();


        /// <summary>
        /// Enables (and disables) notifications and hooks to listener
        /// </summary>
        /// <param name="characteristic"></param>
        /// <param name="useIndicationsIfAvailable"></param>
        /// <returns></returns>
        public static IObservable<GattCharacteristicResult> Notify(this IGattCharacteristic characteristic, bool useIndicationsIfAvailable = false)
            => characteristic
                .EnableNotifications(true, useIndicationsIfAvailable)
                .Select(_ => characteristic.WhenNotificationReceived())
                .Switch()
                .Finally(() => characteristic.EnableNotifications(false).Subscribe());


        /// <summary>
        ///
        /// </summary>
        /// <param name="characteristic"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WriteBlob(this IGattCharacteristic characteristic, Stream stream) => Observable.Create<IGattCharacteristic>(ob =>
            characteristic
                .WriteBlobWithProgress(stream)
                .Subscribe(
                    _ => { },
                    ex => ob.OnError(ex),
                    () => ob.Respond(characteristic)
                )
        );


        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="ch">The characteristic to write on</param>
        /// <param name="stream">The stream to send</param>
        public static IObservable<BleWriteSegment> WriteBlobWithProgress(this IGattCharacteristic ch, Stream stream) => Observable.Create<BleWriteSegment>(async (ob, ct) =>
        {
            var mtu = ch.Service.Peripheral.MtuSize;
            var buffer = new byte[mtu];
            var read = stream.Read(buffer, 0, buffer.Length);
            var pos = read;
            var len = Convert.ToInt32(stream.Length);
            var remaining = 0;

            while (!ct.IsCancellationRequested && read > 0)
            {
                await ch
                    .Write(buffer)
                    .ToTask(ct)
                    .ConfigureAwait(false);

                //if (this.Value != buffer)
                //{
                //    trans.Abort();
                //    throw new GattReliableWriteTransactionException("There was a mismatch response");
                //}
                var seg = new BleWriteSegment(buffer, pos, len);
                ob.OnNext(seg);

                remaining = len - pos;
                if (remaining > 0 && remaining < mtu)
                {
                    // readjust buffer -- we don't want to send extra garbage
                    buffer = new byte[remaining];
                }

                read = stream.Read(buffer, 0, buffer.Length);
                pos += read;
            }
            ob.OnCompleted();

            return Disposable.Empty;
        });


        public static IObservable<GattCharacteristicResult> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
            => Observable
                .Interval(timeSpan)
                .Select(_ => character.Read())
                .Switch();


        public static bool CanRead(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Read);
        public static bool CanWriteWithResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Write);
        public static bool CanWriteWithoutResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
        public static bool CanWrite(this IGattCharacteristic ch) =>
            ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse) ||
            ch.Properties.HasFlag(CharacteristicProperties.Write) ||
            ch.Properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites);


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
