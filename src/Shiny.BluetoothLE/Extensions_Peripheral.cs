using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public static class PeripheralExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetCharacteristicsForService(this IPeripheral peripheral, string serviceUuid)
            => peripheral
                .GetKnownService(serviceUuid)
                .Select(x => x.DiscoverCharacteristics()).Switch();


        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="sendHook"></param>
        /// <param name="useIndicationIfAvailable"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> Notify(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, bool sendHook = false, bool useIndicationIfAvailable = false)
            => peripheral
                .WhenConnected()
                .Select(_ => peripheral.WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid))
                .Switch()
                .Select(x => x.Notify(sendHook, useIndicationIfAvailable))
                .Merge();


        /// <summary>
        /// An easy wrapper around checking peripheral status == ConnectionState.Connected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns>Return true if connected, otherwise false</returns>
        public static bool IsConnected(this IPeripheral peripheral) => peripheral.Status == ConnectionState.Connected;


        /// <summary>
        /// An easy wrapper around checking peripheral status == ConnectionState.Disconnected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns>Return true if disconnected, otherwise false</returns>
        public static bool IsDisconnected(this IPeripheral peripheral) => !peripheral.IsConnected();


        /// <summary>
        /// Starts connection process if not already connecteds
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="connectionConfig"></param>
        /// <returns>True if connection attempt was sent, otherwise false</returns>
        public static bool ConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null)
        {
            if (peripheral.Status == ConnectionState.Disconnected)
            {
                peripheral.Connect(config);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Continuously reads RSSI from a connected peripheral
        /// WARNING: you really don't want to run this with an Android GATT connection
        /// </summary>
        /// <param name="peripheral">The peripheral to read RSSI</param>
        /// <param name="readInterval">The interval to poll the RSSI - defaults to 1 second if not set</param>
        /// <returns>RSSI value every read interval</returns>
        public static IObservable<int> ReadRssiContinuously(this IPeripheral peripheral, TimeSpan? readInterval = null) => Observable
            .Interval(readInterval ?? TimeSpan.FromSeconds(1))
            .Where(_ => peripheral.IsConnected())
            .Select(_ => peripheral.ReadRssi())
            .Switch();


        /// <summary>
        /// Waits for connection to actually happen
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ConnectWait(this IPeripheral peripheral, ConnectionConfig? config = null)
            => Observable.Create<IPeripheral>(ob =>
            {
                var sub1 = peripheral
                    .WhenConnected()
                    .Take(1)
                    .Subscribe(_ => ob.Respond(peripheral));

                var sub2 = peripheral
                    .WhenConnectionFailed()
                    .Subscribe(ob.OnError);

                peripheral.ConnectIf(config);
                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                    if (peripheral.Status != ConnectionState.Connected)
                        peripheral.CancelConnection();
                };
            });


        /// <summary>
        /// Discover the characteristic and write to it
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="withResponse"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
            => peripheral
                .GetKnownCharacteristics(serviceUuid, characteristicUuid)
                .Select(x => x.Write(data, withResponse))
                .Switch();


        /// <summary>
        /// Discover the characteristic and read it
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid)
            => peripheral
                .GetKnownCharacteristics(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();


        /// <summary>
        /// Get known characteristic(s) without service instance
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IPeripheral peripheral, string serviceUuid, params string[] characteristicIds) =>
            peripheral
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds));


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> WhenConnected(this IPeripheral peripheral) =>
            peripheral
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Connected)
                .Select(_ => peripheral);


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Disconnected)
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> WhenDisconnected(this IPeripheral peripheral) =>
            peripheral
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Disconnected)
                .Select(_ => peripheral);


        /// <summary>
        /// Will call GetKnownCharacteristics when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenKnownCharacteristicsDiscovered(this IPeripheral peripheral, string serviceUuid, params string[] characteristicIds) =>
            peripheral
                .WhenConnected()
                .SelectMany(x => x.GetKnownCharacteristics(serviceUuid, characteristicIds));


        /// <summary>
        /// Get a known service when the peripheral is connected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattService> WhenConnectedGetKnownService(this IPeripheral peripheral, string serviceUuid) =>
            peripheral
                .WhenConnected()
                .Select(x => x.GetKnownService(serviceUuid))
                .Switch();

        /// <summary>
        /// Will discover all services/characteristics when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IPeripheral peripheral) =>
            peripheral
                .WhenConnected()
                .Select(x => peripheral.DiscoverServices())
                .Switch()
                .SelectMany(x => x.DiscoverCharacteristics());


        /// <summary>
        /// Will discover all services/characteristics/descriptors when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IPeripheral peripheral)
            => peripheral.WhenAnyCharacteristicDiscovered().SelectMany(x => x.DiscoverDescriptors());
    }
}
